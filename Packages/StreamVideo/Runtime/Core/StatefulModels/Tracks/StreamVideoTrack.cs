using System;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamVideoTrack : BaseStreamTrack<VideoStreamTrack>
    {
        public StreamVideoTrack(MediaStreamTrack track)
            : base(track)
        {
            //Track.OnVideoReceived += OnVideoReceived;
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
        /// <summary>
        /// Renders this track into a uGUI <see cref="RawImage"/>.
        /// </summary>
        /// <remarks>
        /// Only one render target is active. The last <c>SetRenderTarget</c> call wins and clears the other target.
        /// Call from the main thread. The target is updated when the incoming resolution changes and the texture
        /// instance is replaced. After a reconnect the participant receives a new track
        /// (<see cref="StreamVideo.Core.StatefulModels.IStreamVideoCallParticipant.TrackAdded"/>), so call <c>SetRenderTarget</c> again.
        /// </remarks>
        /// <param name="targetImage">Image that receives the video. Must not be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="targetImage"/> is null.</exception>
        public void SetRenderTarget(RawImage targetImage)
        {
            if (targetImage == null)
            {
                throw new ArgumentNullException(nameof(targetImage));
            }

            _targetImage = targetImage;
            _targetUIToolkitImage = null;
        }

        /// <summary>
        /// Renders this track into a UI Toolkit <see cref="UnityEngine.UIElements.Image"/>.
        /// </summary>
        /// <remarks>
        /// Only one render target is active. The last <c>SetRenderTarget</c> call wins and clears the other target.
        /// Call from the main thread. <see cref="UnityEngine.UIElements.Image.image"/> stays null until the first
        /// frame. The SDK assigns the new texture instance when the incoming resolution changes.
        /// After a reconnect the participant receives a new track
        /// (<see cref="StreamVideo.Core.StatefulModels.IStreamVideoCallParticipant.TrackAdded"/>), so call <c>SetRenderTarget</c> again.
        /// Set <see cref="UnityEngine.UIElements.Image.scaleMode"/> to <see cref="ScaleMode.ScaleAndCrop"/> and size
        /// the element with USS (absolute fill or flex-grow). Without that, the element sizes itself to the texture
        /// and the layout jumps when the resolution changes.
        /// </remarks>
        /// <param name="targetImage">Element that receives the video. Must not be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="targetImage"/> is null.</exception>
        public void SetRenderTarget(UnityEngine.UIElements.Image targetImage)
        {
            if (targetImage == null)
            {
                throw new ArgumentNullException(nameof(targetImage));
            }

            _targetUIToolkitImage = targetImage;
            _targetImage = null;
        }

        internal override void Update()
        {
            base.Update();

            if (_targetImage != null)
            {
                _targetImage.texture = Track.Texture;
                return;
            }

            if (_targetUIToolkitImage != null)
            {
                var texture = Track.Texture;
                if (!ReferenceEquals(_targetUIToolkitImage.image, texture))
                {
                    _targetUIToolkitImage.image = texture;
                }
            }
        }

        protected override void OnDisposing()
        {
#if STREAM_DEBUG_ENABLED
            UnityEngine.Debug.LogWarning("[Participant] Video track disposed.");
#endif
            // if (Track != null)
            // {
            //     Track.OnVideoReceived -= OnVideoReceived;
            // }

            if (_targetUIToolkitImage != null && ReferenceEquals(_targetUIToolkitImage.image, Track.Texture))
            {
                _targetUIToolkitImage.image = null;
            }

            _targetUIToolkitImage = null;

            base.OnDisposing();
        }
        
        //StreamTodo: remove
        internal RenderTexture TargetTexture => throw new NotSupportedException("This property is deprecated");

        private RawImage _targetImage;
        private UnityEngine.UIElements.Image _targetUIToolkitImage;
    }
}