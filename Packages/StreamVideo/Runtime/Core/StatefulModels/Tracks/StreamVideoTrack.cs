using System;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamVideoTrack : BaseStreamTrack<VideoStreamTrack>
    {
        public event Action VideoRotationAngleChanged;
        internal event Action<StreamVideoTrack> TextureStillNull;

        internal string TrackId => Track?.Id;

        public int VideoRotationAngle
        {
            get => _videoRotationAngle;
            internal set
            {
                if (_videoRotationAngle == value)
                {
                    return;
                }

                var prev = _videoRotationAngle;
                _videoRotationAngle = value;
                VideoRotationAngleChanged?.Invoke();
            }
        }
        
        public StreamVideoTrack(MediaStreamTrack track)
            : base(track)
        {
            Track.OnVideoReceived += OnVideoReceived;
            _trackAddedAtRealtime = Time.realtimeSinceStartup;
        }

        private void OnVideoReceived(Texture renderer)
        {
#if STREAM_DEBUG_ENABLED
            if (!_onVideoReceivedLogged)
            {
                _onVideoReceivedLogged = true;
                Debug.LogWarning(
                    $"[LateVideoDiag] StreamVideoTrack OnVideoReceived trackId={Track?.Id}, " +
                    $"renderer={FormatTexture(renderer)}, targetBound={_targetImage != null}");
            }
#endif

            if (_targetImage == null || renderer == null)
            {
                return;
            }

            _targetImage.texture = renderer;
        }

        //StreamTodo: can we remove Unity dependency? 
        public void SetRenderTarget(RawImage targetImage)
        {
            if (targetImage == null)
            {
                throw new ArgumentNullException(nameof(targetImage));
            }

            _targetImage = targetImage;

#if STREAM_DEBUG_ENABLED
            Debug.LogWarning(
                $"[LateVideoDiag] StreamVideoTrack SetRenderTarget bound, trackId={Track?.Id}, " +
                $"target={targetImage.name}, hasTexture={Track?.Texture != null}, " +
                $"enabled={Track?.Enabled}, readyState={GetReadyStateDebug()}");
#endif
        }

        internal override void Update()
        {
            base.Update();
            
            if (_targetImage == null)
            {
                return;
            }
            
            _targetImage.texture = Track.Texture;

            CheckTextureReadiness();
        }

        protected override void OnDisposing()
        {
#if STREAM_DEBUG_ENABLED
            UnityEngine.Debug.LogWarning("[Participant] Video track disposed.");
#endif
            if (Track != null)
            {
                Track.OnVideoReceived -= OnVideoReceived;
            }

            base.OnDisposing();
        }
        
        //StreamTodo: remove
        internal RenderTexture TargetTexture => throw new NotSupportedException("This property is deprecated");

        private const float TextureNullWarningDelaySeconds = 3f;

        private float _trackAddedAtRealtime;
        private bool _textureReadyLogged;
        private bool _textureNullWarningLogged;

        private void CheckTextureReadiness()
        {
            var texture = Track?.Texture;
            if (texture != null)
            {
                if (_textureReadyLogged)
                {
                    return;
                }

                _textureReadyLogged = true;
#if STREAM_DEBUG_ENABLED
                var elapsed = Time.realtimeSinceStartup - _trackAddedAtRealtime;
                Debug.LogWarning(
                    $"[LateVideoDiag] StreamVideoTrack texture ready after {elapsed:F2}s, " +
                    $"trackId={Track.Id}, size={texture.width}x{texture.height}, " +
                    $"targetBound={_targetImage != null}, uiTextureSet={_targetImage?.texture != null}");
#endif
                return;
            }

            if (_textureNullWarningLogged)
            {
                return;
            }

            var waitTime = Time.realtimeSinceStartup - _trackAddedAtRealtime;
            if (waitTime < TextureNullWarningDelaySeconds)
            {
                return;
            }

            _textureNullWarningLogged = true;
#if STREAM_DEBUG_ENABLED
            Debug.LogWarning(
                $"[LateVideoDiag] StreamVideoTrack texture still null after {waitTime:F2}s, " +
                $"trackId={Track?.Id}, enabled={Track?.Enabled}, readyState={GetReadyStateDebug()}, " +
                $"targetBound={_targetImage != null}");
#endif

            TextureStillNull?.Invoke(this);
        }

#if STREAM_DEBUG_ENABLED
        private bool _onVideoReceivedLogged;

        private string GetReadyStateDebug()
        {
            try
            {
                return Track?.ReadyState.ToString() ?? "null";
            }
            catch (InvalidOperationException)
            {
                return "disposed";
            }
        }

        private static string FormatTexture(Texture texture)
            => texture != null ? $"{texture.width}x{texture.height}" : "null";
#endif

        private RawImage _targetImage;
        private int _videoRotationAngle;
    }
}
