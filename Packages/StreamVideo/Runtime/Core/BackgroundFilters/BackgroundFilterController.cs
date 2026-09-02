using System;
using StreamVideo.Libs.Logs;
using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    internal sealed class BackgroundFilterController : IDisposable
    {
        public event Action<BackgroundFilterPerformance> PerformanceChanged;
        public event Action<Texture> PreviewTextureChanged;

        public BackgroundFilter ActiveFilter { get; private set; }

        public bool IsSupported => _segmenter != null && _segmenter.IsSupported;

        public bool IsCompositing => ActiveFilter != null && IsSupported && !_paused && _segmenter.HasMask;

        public BackgroundFilterPerformance Performance => _scheduler.Performance;

        public BackgroundFilterController(ILogs logs, IPersonSegmenter segmenter = null)
        {
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _segmenter = segmenter ?? PersonSegmenterFactory.Create(_logs);
            CameraOrientationDebug.Log(_logs, "controller.init",
                "segmenter=" + _segmenter.GetType().Name + " supported=" + _segmenter.IsSupported
                + " | " + CameraOrientationDebug.DescribeScreen());
        }

        public void SetFilter(BackgroundFilter filter)
        {
            if (filter != null && !IsSupported)
            {
                _logs.Warning(
                    "Background filter is not supported on this platform or device. The request was ignored.");
                ActiveFilter = null;
                return;
            }

            if (ActiveFilter == filter)
            {
                return;
            }

            ActiveFilter = filter;

            if (filter == null)
            {
                _compositor.SetMask(null);
                _segmenter.Pause();
                ReleasePreview();
                if (!_scheduler.ShouldDisable)
                {
                    _scheduler.Reset(BlurIntensity.Medium);
                }

                PublishPerformanceIfChanged();
                return;
            }

            _requestedIntensity = filter.Intensity;
            _scheduler.Reset(_requestedIntensity);
            _compositor.SetIntensity(_scheduler.EffectiveIntensity);
            _segmenter.Resume();
            _paused = false;
            _frameIndex = 0;
            CameraOrientationDebug.Log(_logs, "controller.setFilter",
                "filter=" + (filter == null ? "null" : filter.Kind + "/" + filter.Intensity)
                + " supported=" + IsSupported
                + " segmenter=" + _segmenter.GetType().Name);
            PublishPerformanceIfChanged();
        }

        public void Composite(Texture source, RenderTexture destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            if (ActiveFilter == null || !IsSupported || _paused)
            {
                Graphics.Blit(source, destination);
                SetPreview(destination);
                LogCompositeOrientation(source, destination, "composite.passthrough");
                return;
            }

            PumpAndroidMask();

            if (_scheduler.ShouldSegment(_frameIndex))
            {
                _segmenter.RequestSegmentation(source);
            }

            _frameIndex++;
            UpdateSchedulerFromFrameTime();

            if (_scheduler.ShouldDisable)
            {
                _logs.Warning("Background filter disabled because publish FPS could not be maintained.");
                SetFilter(null);
                Graphics.Blit(source, destination);
                SetPreview(destination);
                return;
            }

            if (!_segmenter.HasMask)
            {
                Graphics.Blit(source, destination);
                SetPreview(destination);
                LogCompositeOrientation(source, destination, "composite.waitingMask");
                return;
            }

            _compositor.SetMask(_segmenter.MaskTexture);
            _compositor.SetIntensity(_scheduler.EffectiveIntensity);
            _compositor.Apply(source, destination);
            SetPreview(destination);
            LogCompositeOrientation(source, destination, "composite.apply");
        }

        public Texture GetPreviewTexture() => _previewTexture;

        public void Pause()
        {
            _paused = true;
            _segmenter.Pause();
        }

        public void Resume()
        {
            _paused = false;
            if (ActiveFilter != null && IsSupported)
            {
                _segmenter.Resume();
            }
        }

        public void Dispose()
        {
            SetFilter(null);
            _compositor.Release();
            _segmenter.Dispose();
            ReleasePreview();
        }

        private readonly ILogs _logs;
        private readonly IPersonSegmenter _segmenter;
        private readonly BackgroundCompositor _compositor = new BackgroundCompositor();
        private readonly FilterFrameScheduler _scheduler = new FilterFrameScheduler();

        private const float TargetFrameSeconds = 1f / 30f;

        private BlurIntensity _requestedIntensity = BlurIntensity.Medium;
        private BackgroundFilterPerformance _lastPublishedPerformance;
        private Texture _previewTexture;
        private int _frameIndex;
        private int _sampleFrames;
        private float _sampleSeconds;
        private bool _paused;

        private void LogCompositeOrientation(Texture source, RenderTexture destination, string checkpoint)
        {
            var webcam = source as WebCamTexture;
            var mask = _segmenter.MaskTexture;
            var payload = CameraOrientationDebug.DescribeScreen()
                + " | " + (webcam != null
                    ? CameraOrientationDebug.DescribeWebCam(webcam)
                    : CameraOrientationDebug.DescribeTexture("source", source))
                + " | " + CameraOrientationDebug.DescribeTexture("dest", destination)
                + " | " + CameraOrientationDebug.DescribeTexture("mask", mask)
                + " | filter=" + (ActiveFilter == null ? "null" : ActiveFilter.Kind + "/" + ActiveFilter.Intensity)
                + " hasMask=" + _segmenter.HasMask
                + " paused=" + _paused
                + " compositing=" + IsCompositing
                + " mlkitRotationDegrees=0 (not applied)";
            CameraOrientationDebug.Log(_logs, checkpoint, payload);
        }

        private void PumpAndroidMask()
        {
            if (_segmenter is AndroidMlKitPersonSegmenter androidSegmenter)
            {
                androidSegmenter.PumpPendingMask();
            }
        }

        private void UpdateSchedulerFromFrameTime()
        {
            var frameSeconds = Time.unscaledDeltaTime;
            if (frameSeconds <= 0f)
            {
                return;
            }

            _sampleSeconds += frameSeconds;
            _sampleFrames++;
            if (_sampleSeconds < 1f)
            {
                return;
            }

            // 1 = holding 30 fps publish, < 0.75 = sustained frame-time budget miss
            var averageFrameSeconds = _sampleSeconds / Mathf.Max(1, _sampleFrames);
            var ratio = TargetFrameSeconds / Mathf.Max(averageFrameSeconds, 0.0001f);
            _scheduler.RecordFpsRatio(Mathf.Clamp(ratio, 0f, 2f), _sampleSeconds);
            _sampleSeconds = 0f;
            _sampleFrames = 0;

            if (_scheduler.ShouldDisable)
            {
                PublishPerformanceIfChanged();
                return;
            }

            _compositor.SetIntensity(_scheduler.EffectiveIntensity);
            PublishPerformanceIfChanged();
        }

        private void PublishPerformanceIfChanged()
        {
            var current = _scheduler.Performance;
            if (current.Degraded == _lastPublishedPerformance.Degraded
                && current.Reason == _lastPublishedPerformance.Reason)
            {
                return;
            }

            _lastPublishedPerformance = current;
            PerformanceChanged?.Invoke(current);
        }

        private void SetPreview(Texture texture)
        {
            if (_previewTexture == texture)
            {
                return;
            }

            _previewTexture = texture;
            PreviewTextureChanged?.Invoke(texture);
        }

        private void ReleasePreview()
        {
            if (_previewTexture == null)
            {
                return;
            }

            _previewTexture = null;
            PreviewTextureChanged?.Invoke(null);
        }
    }
}
