using System;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamVideoTrack : BaseStreamTrack<VideoStreamTrack>
    {
        public event Action VideoRotationAngleChanged;

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
        }

        private void OnVideoReceived(Texture renderer)
        {
            if (_targetImage == null)
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

        private RawImage _targetImage;
        private int _videoRotationAngle;
    }
}