using System;
using StreamVideo.Libs.Logs;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
#endif
using Object = UnityEngine.Object;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// iOS Vision person segmenter (<c>VNGeneratePersonSegmentationRequest</c>, iOS 15+).
    /// Same pipeline as Android for readback, but Vision is fed webcam-space pixels
    /// (no CPU rotate). Stream's Swift SDK does the same: the mask size is quality-based
    /// and typically landscape, so rotating to portrait then stretching the mask onto
    /// that buffer pins the person to the center. Metal AsyncGPUReadback is Y-flipped
    /// once into the GLES/Bitmap y-down layout, then flipped again on Texture2D upload
    /// so compositor UVs match iOS <c>WebCamTexture</c> (regular 2D, UV y=0 at bottom).
    /// Quality is Balanced on A12+ (8-core Neural Engine) and Fast on older devices,
    /// matching Stream's Swift SDK. Native <c>perform</c> runs off the Unity thread.
    /// <see cref="TryCreate"/> only checks the iOS version. The Vision client is created
    /// on the first <see cref="Resume"/> / <see cref="RequestSegmentation"/>.
    /// </summary>
    internal sealed class IosVisionPersonSegmenter : IPersonSegmenter
    {
        public const int MinMaskInputSize = 256;

        /// <summary>
        /// Returns a deferred segmenter on iOS 15+ device builds. Does not create the
        /// Vision request until the filter is enabled.
        /// </summary>
        public static bool TryCreate(ILogs logs, out IosVisionPersonSegmenter segmenter)
        {
            segmenter = null;
#if UNITY_IOS && !UNITY_EDITOR
            if (!NativeIsSupported())
            {
                logs?.Warning(
                    "Background filter: person segmentation requires iOS 15 or later on this device.");
                return false;
            }

            segmenter = new IosVisionPersonSegmenter(logs);
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Balanced Vision quality on A12+ (iPhone XS / iPad Pro 3rd gen and newer).
        /// Fast on A11 and below, simulators, and unknown identifiers.
        /// </summary>
        internal static bool UsesBalancedQuality(string deviceModel)
        {
            if (string.IsNullOrEmpty(deviceModel))
            {
                return false;
            }

            return HasIdentifierMajorAtLeast(deviceModel, "iPhone", 11)
                || HasIdentifierMajorAtLeast(deviceModel, "iPad", 8);
        }

        public bool IsSupported => _isSupported;

        public bool HasMask => _maskTexture != null && _hasMask;

        public Texture MaskTexture => _maskTexture;

        public void RequestSegmentation(Texture source)
        {
            if (_disposed || _paused || source == null || _readbackInFlight || !EnsureCreated())
            {
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            EnsureDownscaleRt(source);
            if (_downscaleRt == null)
            {
                return;
            }

            Graphics.Blit(source, _downscaleRt);
            _lastSource = source;
#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
            _lastSourceRotation = GetSourceRotationDegrees(source);
            LogSubmitOrientation(source);
#endif

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                _readbackInFlight = true;
                try
                {
                    AsyncGPUReadback.Request(_downscaleRt, 0, TextureFormat.RGBA32, OnReadback);
                }
                catch (Exception e)
                {
                    _readbackInFlight = false;
                    _logs?.Warning("Background filter: mask input readback request failed: " + e.Message);
                }

                return;
            }

            ReadbackSynchronouslyAndProcess();
#endif
        }

        public void PumpPendingMask()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_disposed || _paused || !_nativeCreated)
            {
                return;
            }

            if (_maskScratch == null)
            {
                _maskScratch = new byte[MinMaskInputSize * MinMaskInputSize];
            }

            var copied = NativeTakeMaskIfNew(_maskScratch, _maskScratch.Length, out var width, out var height,
                out var rotation);
            if (copied < 0)
            {
                _maskScratch = new byte[-copied];
                copied = NativeTakeMaskIfNew(_maskScratch, _maskScratch.Length, out width, out height, out rotation);
            }

            if (copied > 0 && width > 0 && height > 0)
            {
                var webcamMask = PersonMaskOrientation.UnrotateClockwise(_maskScratch, width, height, 1, rotation,
                    out var webcamWidth, out var webcamHeight);
                UploadMask(webcamMask, webcamWidth, webcamHeight);
            }

            TrySubmitPending();
#endif
        }

        public void Pause()
        {
            _paused = true;
            // Suspended GPU readbacks may never callback; do not block Resume.
            _readbackInFlight = false;
#if UNITY_IOS && !UNITY_EDITOR
            _hasPendingRgba = false;
#endif
        }

        public void Resume()
        {
            if (_disposed || !EnsureCreated())
            {
                return;
            }

            _paused = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _paused = true;
            _lastSource = null;
#if STREAM_DEBUG_ENABLED
            CameraOrientationDebug.Flush(_logs);
#endif

#if UNITY_IOS && !UNITY_EDITOR
            _hasPendingRgba = false;
            _pendingRgba = null;
            _maskScratch = null;
            DestroyNative();
            DestroyTexture(ref _syncReadbackTexture);
#endif
            DestroyTexture(ref _maskTexture);

            if (!_readbackInFlight)
            {
                ReleaseDownscaleRt();
            }
        }

        private readonly ILogs _logs;
        private bool _disposed;
        private bool _paused;
        private bool _hasMask;
        private bool _isSupported = true;
        private bool _readbackInFlight;
        private Texture2D _maskTexture;
        private RenderTexture _downscaleRt;
        private Texture _lastSource;
#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
        private int _lastSourceRotation;
#endif
#if UNITY_IOS && !UNITY_EDITOR
        private bool _createAttempted;
        private bool _nativeCreated;
        private Texture2D _syncReadbackTexture;
        private byte[] _pendingRgba;
        private byte[] _maskScratch;
        private int _pendingWidth;
        private int _pendingHeight;
        private bool _hasPendingRgba;
#endif

        private static bool HasIdentifierMajorAtLeast(string identifier, string prefix, int minMajor)
        {
            if (identifier.Length <= prefix.Length
                || !identifier.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var rest = identifier.Substring(prefix.Length);
            var comma = rest.IndexOf(',');
            var majorText = comma >= 0 ? rest.Substring(0, comma) : rest;
            return int.TryParse(majorText, out var major) && major >= minMajor;
        }

        private IosVisionPersonSegmenter(ILogs logs)
        {
            _logs = logs;
        }

        private bool EnsureCreated()
        {
            if (_disposed)
            {
                return false;
            }

#if UNITY_IOS && !UNITY_EDITOR
            if (_nativeCreated)
            {
                return true;
            }

            if (_createAttempted)
            {
                return false;
            }

            _createAttempted = true;
            try
            {
                if (!NativeCreate(UsesBalancedQuality(SystemInfo.deviceModel) ? 1 : 0))
                {
                    FailCreate("Background filter: failed to create the Vision person segmenter.");
                    return false;
                }

                _nativeCreated = true;
                return true;
            }
            catch (Exception e)
            {
                DestroyNative();
                FailCreate("Background filter: Vision init failed: " + e.Message);
                return false;
            }
#else
            return true;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool _StreamVisionPersonSegmenter_IsSupported();

        [DllImport("__Internal")]
        private static extern bool _StreamVisionPersonSegmenter_Create(int useBalanced);

        [DllImport("__Internal")]
        private static extern void _StreamVisionPersonSegmenter_Destroy();

        [DllImport("__Internal")]
        private static extern bool _StreamVisionPersonSegmenter_IsBusy();

        [DllImport("__Internal")]
        private static extern void _StreamVisionPersonSegmenter_ProcessAsync(byte[] rgba, int length, int width,
            int height, int rotationDegrees);

        [DllImport("__Internal")]
        private static extern int _StreamVisionPersonSegmenter_TakeMaskIfNew(byte[] dest, int destLength,
            out int width, out int height, out int rotation);

        private static bool NativeIsSupported() => _StreamVisionPersonSegmenter_IsSupported();

        private static bool NativeCreate(int useBalanced) => _StreamVisionPersonSegmenter_Create(useBalanced);

        private static void NativeDestroy() => _StreamVisionPersonSegmenter_Destroy();

        private static bool NativeIsBusy() => _StreamVisionPersonSegmenter_IsBusy();

        private static void NativeProcessAsync(byte[] rgba, int length, int width, int height, int rotationDegrees)
            => _StreamVisionPersonSegmenter_ProcessAsync(rgba, length, width, height, rotationDegrees);

        private static int NativeTakeMaskIfNew(byte[] dest, int destLength, out int width, out int height,
            out int rotation)
            => _StreamVisionPersonSegmenter_TakeMaskIfNew(dest, destLength, out width, out height, out rotation);

        private void FailCreate(string message)
        {
            _isSupported = false;
            _logs?.Warning(message);
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            _readbackInFlight = false;

            try
            {
                if (_disposed || _paused || !_nativeCreated)
                {
                    return;
                }

                if (request.hasError)
                {
                    _logs?.Warning("Background filter: mask input readback failed.");
                    return;
                }

                var data = request.GetData<byte>();
                var length = data.Length;
                if (_pendingRgba == null || _pendingRgba.Length != length)
                {
                    _pendingRgba = new byte[length];
                }

                data.CopyTo(_pendingRgba);
                _pendingWidth = request.width;
                _pendingHeight = request.height;
                var packedBytes = _pendingWidth * _pendingHeight * 4;
                var isOpenGles = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                    || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
                if (packedBytes > 0 && _pendingRgba.Length >= packedBytes
                    && PersonMaskOrientation.NeedsAsyncGpuReadbackYFlip(SystemInfo.graphicsUVStartsAtTop, isOpenGles))
                {
                    PersonMaskOrientation.FlipVertical(_pendingRgba, _pendingWidth, _pendingHeight, 4);
                }

                _hasPendingRgba = true;
                TrySubmitPending();
            }
            finally
            {
                if (_disposed)
                {
                    ReleaseDownscaleRt();
                }
            }
        }

        private void TrySubmitPending()
        {
            if (_disposed || !_hasPendingRgba || _paused || !_nativeCreated)
            {
                return;
            }

            if (NativeIsBusy())
            {
                return;
            }

            SubmitRgba(_pendingRgba, _pendingWidth, _pendingHeight);
            _hasPendingRgba = false;
        }

        private void ReadbackSynchronouslyAndProcess()
        {
            var width = _downscaleRt.width;
            var height = _downscaleRt.height;
            if (_syncReadbackTexture == null || _syncReadbackTexture.width != width ||
                _syncReadbackTexture.height != height)
            {
                if (_syncReadbackTexture != null)
                {
                    Object.Destroy(_syncReadbackTexture);
                }

                _syncReadbackTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }

            var prev = RenderTexture.active;
            RenderTexture.active = _downscaleRt;
            _syncReadbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            _syncReadbackTexture.Apply(false, false);
            RenderTexture.active = prev;

            SubmitRgba(_syncReadbackTexture.GetRawTextureData(), width, height);
        }

        private void SubmitRgba(byte[] rgba, int width, int height)
        {
            if (_disposed || rgba == null || !_nativeCreated)
            {
                return;
            }

            // Keep webcam UVs. Vision's output size follows qualityLevel, not the input
            // aspect; rotating upright (Android ML Kit path) stretches a landscape mask
            // onto a portrait buffer and the cutout sticks to the center.
#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
            var webcam = _lastSource as WebCamTexture;
            var canRead = CameraOrientationDebug.CanReadWebCamOrientation(webcam);
            var webcamRot = canRead ? webcam.videoRotationAngle : -1;
            CameraOrientationDebug.Log(_logs, "vision.submit",
                "rgba=" + width + "x" + height + " bytes=" + rgba.Length
                + " visionRotationDegrees=0 (webcam space, not rotated)"
                + " webcamRot=" + webcamRot
                + " mirrored=" + (canRead && webcam.videoVerticallyMirrored)
                + " gfx=" + SystemInfo.graphicsDeviceType
                + " asyncReadback=" + SystemInfo.supportsAsyncGPUReadback);
#endif
            NativeProcessAsync(rgba, width * height * 4, width, height, 0);
        }

#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
        private static int GetSourceRotationDegrees(Texture source)
        {
            var webcam = source as WebCamTexture;
            if (!CameraOrientationDebug.CanReadWebCamOrientation(webcam))
            {
                return 0;
            }

            return PersonMaskOrientation.NormalizeClockwiseDegrees(webcam.videoRotationAngle);
        }
#endif

        private void UploadMask(byte[] mask, int width, int height)
        {
            if (_disposed || mask == null || mask.Length < width * height)
            {
                return;
            }

            if (_maskTexture == null || _maskTexture.width != width || _maskTexture.height != height)
            {
                if (_maskTexture != null)
                {
                    Object.Destroy(_maskTexture);
                }

                _maskTexture = new Texture2D(width, height, TextureFormat.R8, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "StreamVisionPersonMask",
                };
            }

            var isOpenGles = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
            if (PersonMaskOrientation.NeedsYFlipFromBitmapLayoutToTexture2D(isOpenGles))
            {
                PersonMaskOrientation.FlipVertical(mask, width, height, 1);
            }

            _maskTexture.SetPixelData(mask, 0);
            _maskTexture.Apply(false, false);
            _hasMask = true;

#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
            var hits = 0;
            var needed = width * height;
            for (var i = 0; i < needed; i++)
            {
                if (mask[i] > 128)
                {
                    hits++;
                }
            }

            var inputW = _downscaleRt != null ? _downscaleRt.width : 0;
            var inputH = _downscaleRt != null ? _downscaleRt.height : 0;
            CameraOrientationDebug.RecordMask(_logs,
                "mask=" + width + "x" + height
                + " input=" + inputW + "x" + inputH
                + " aspectMatch=" + (width * inputH == height * inputW)
                + " | " + CameraOrientationDebug.DescribeWebCam(_lastSource as WebCamTexture),
                hits / (float)Mathf.Max(1, needed));
#endif
        }

        private void EnsureDownscaleRt(Texture source)
        {
            if (_disposed)
            {
                return;
            }

            GetMaskInputSize(source.width, source.height, out var width, out var height);
            if (_downscaleRt != null && _downscaleRt.width == width && _downscaleRt.height == height)
            {
                if (!_downscaleRt.IsCreated())
                {
                    _downscaleRt.Create();
                }

                return;
            }

            ReleaseDownscaleRt();

            _downscaleRt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "StreamBgFilterMaskInput",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _downscaleRt.Create();
        }

        private void DestroyNative()
        {
            if (!_nativeCreated)
            {
                return;
            }

            try
            {
                NativeDestroy();
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: Vision destroy failed: " + e.Message);
            }

            _nativeCreated = false;
        }

#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
        private void LogSubmitOrientation(Texture source)
        {
            var webcam = source as WebCamTexture;
            CameraOrientationDebug.Log(_logs, "vision.downscale",
                CameraOrientationDebug.DescribeTexture("source", source)
                + " | " + CameraOrientationDebug.DescribeTexture("downscale", _downscaleRt)
                + " | " + (webcam != null
                    ? CameraOrientationDebug.DescribeWebCam(webcam)
                    : "sourceIsWebCam=false")
                + " blit=Graphics.Blit(source, downscale); Vision runs in webcam space"
                + " sourceRot=" + _lastSourceRotation);
        }
#endif

        private static void GetMaskInputSize(int sourceWidth, int sourceHeight, out int width, out int height)
        {
            sourceWidth = Mathf.Max(2, sourceWidth);
            sourceHeight = Mathf.Max(2, sourceHeight);
            var shortSide = Mathf.Min(sourceWidth, sourceHeight);
            if (shortSide <= MinMaskInputSize)
            {
                width = sourceWidth;
                height = sourceHeight;
                return;
            }

            var scale = MinMaskInputSize / (float)shortSide;
            width = Mathf.Max(2, Mathf.RoundToInt(sourceWidth * scale));
            height = Mathf.Max(2, Mathf.RoundToInt(sourceHeight * scale));
        }
#endif

        private void ReleaseDownscaleRt()
        {
            if (_downscaleRt == null)
            {
                return;
            }

            try
            {
                if (RenderTexture.active == _downscaleRt)
                {
                    RenderTexture.active = null;
                }

                _downscaleRt.Release();
                Object.Destroy(_downscaleRt);
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: failed to release mask input RT: " + e.Message);
            }

            _downscaleRt = null;
        }

        private void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            try
            {
                Object.Destroy(texture);
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: failed to destroy texture: " + e.Message);
            }

            texture = null;
        }
    }
}
