using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Views
{
    /// <summary>
    /// Renders a video texture inside a container so that it covers the whole container (cropping the overflow),
    /// taking into account the rotation reported by the camera or the remote video track.
    /// </summary>
    internal sealed class VideoSurface
    {
        public VideoSurface(VisualElement container)
        {
            _container = container;
            _image = new Image
            {
                scaleMode = ScaleMode.StretchToFill,
                pickingMode = PickingMode.Ignore,
            };
            _image.style.position = Position.Absolute;
            _container.Add(_image);
        }

        /// <summary>
        /// Set the texture to render. Call every frame, the layout is only recalculated when something changes.
        /// </summary>
        /// <param name="texture">Video texture or null to render nothing</param>
        /// <param name="rotationAngle">Clockwise rotation required to display the video upright</param>
        public void SetSource(Texture texture, int rotationAngle)
        {
            if (_image.image != texture)
            {
                _image.image = texture;
            }

            _image.visible = texture != null;
            if (texture == null)
            {
                return;
            }

            var containerSize = _container.contentRect.size;
            var textureSize = new Vector2(texture.width, texture.height);
            if (containerSize == _lastContainerSize && textureSize == _lastTextureSize &&
                rotationAngle == _lastRotationAngle)
            {
                return;
            }

            _lastContainerSize = containerSize;
            _lastTextureSize = textureSize;
            _lastRotationAngle = rotationAngle;

            // Negated comparisons also reject NaN which is reported before the first layout pass
            if (!(containerSize.x > 0) || !(containerSize.y > 0) || textureSize.x <= 0 || textureSize.y <= 0)
            {
                return;
            }

            var isRotatedSideways = Mathf.Abs(rotationAngle) % 180 == 90;
            var displayedWidth = isRotatedSideways ? textureSize.y : textureSize.x;
            var displayedHeight = isRotatedSideways ? textureSize.x : textureSize.y;
            var coverScale = Mathf.Max(containerSize.x / displayedWidth, containerSize.y / displayedHeight);

            // Size the element before rotation. Rotation is applied around its center.
            var width = textureSize.x * coverScale;
            var height = textureSize.y * coverScale;

            var style = _image.style;
            style.width = width;
            style.height = height;
            style.left = (containerSize.x - width) / 2f;
            style.top = (containerSize.y - height) / 2f;
            style.rotate = new Rotate(new Angle(rotationAngle, AngleUnit.Degree));
        }

        private readonly VisualElement _container;
        private readonly Image _image;

        private Vector2 _lastContainerSize;
        private Vector2 _lastTextureSize;
        private int _lastRotationAngle;
    }
}
