using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Views
{
    /// <summary>
    /// UI Toolkit image that fills its container and cover-crops the video.
    /// Remote tracks are assigned by <c>StreamVideoTrack.SetRenderTarget</c>. Local preview textures are resized
    /// in place, so <see cref="SetSource"/> marks the image dirty when the size changes.
    /// </summary>
    internal sealed class VideoSurface
    {
        public Image Image => _image;

        public VideoSurface(VisualElement container)
        {
            _image = new Image
            {
                scaleMode = ScaleMode.ScaleAndCrop,
                pickingMode = PickingMode.Ignore,
            };
            _image.style.position = Position.Absolute;
            _image.style.left = 0;
            _image.style.top = 0;
            _image.style.right = 0;
            _image.style.bottom = 0;
            container.Add(_image);
        }

        /// <summary>
        /// Assign an externally owned texture, such as the local camera preview.
        /// </summary>
        /// <param name="texture">Video texture, or null to render nothing</param>
        public void SetSource(Texture texture)
        {
            if (!ReferenceEquals(_image.image, texture))
            {
                _image.image = texture;
                _lastWidth = texture != null ? texture.width : 0;
                _lastHeight = texture != null ? texture.height : 0;
                return;
            }

            if (texture == null)
            {
                return;
            }

            if (texture.width == _lastWidth && texture.height == _lastHeight)
            {
                return;
            }

            _lastWidth = texture.width;
            _lastHeight = texture.height;
            _image.MarkDirtyRepaint();
        }

        private readonly Image _image;
        private int _lastWidth;
        private int _lastHeight;
    }
}
