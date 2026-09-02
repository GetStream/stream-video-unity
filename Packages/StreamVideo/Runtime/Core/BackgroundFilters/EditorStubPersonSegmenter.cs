using UnityEngine;
using Object = UnityEngine.Object;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Fake centered-ellipse person mask so the compositor can run in the Editor.
    /// </summary>
    internal sealed class EditorStubPersonSegmenter : IPersonSegmenter
    {
        public bool IsSupported => true;

        public bool HasMask => _maskTexture != null;

        public Texture MaskTexture => _maskTexture;

        public void RequestSegmentation(Texture source)
        {
            if (source == null)
            {
                return;
            }

            var width = Mathf.Max(16, source.width / 4);
            var height = Mathf.Max(16, source.height / 4);
            EnsureMask(width, height);
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Dispose()
        {
            if (_maskTexture == null)
            {
                return;
            }

            Object.Destroy(_maskTexture);
            _maskTexture = null;
        }

        private Texture2D _maskTexture;

        private void EnsureMask(int width, int height)
        {
            if (_maskTexture != null && _maskTexture.width == width && _maskTexture.height == height)
            {
                return;
            }

            if (_maskTexture != null)
            {
                Object.Destroy(_maskTexture);
            }

            _maskTexture = new Texture2D(width, height, TextureFormat.R8, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "StreamEditorPersonMask",
            };

            var pixels = new byte[width * height];
            var centerX = (width - 1) * 0.5f;
            var centerY = (height - 1) * 0.55f;
            var radiusX = width * 0.28f;
            var radiusY = height * 0.42f;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = (x - centerX) / radiusX;
                    var ny = (y - centerY) / radiusY;
                    var inside = nx * nx + ny * ny <= 1f;
                    pixels[y * width + x] = inside ? (byte)255 : (byte)0;
                }
            }

            _maskTexture.SetPixelData(pixels, 0);
            _maskTexture.Apply(false, false);
        }
    }
}
