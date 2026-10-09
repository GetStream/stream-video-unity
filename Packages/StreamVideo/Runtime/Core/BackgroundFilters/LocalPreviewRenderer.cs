using System;
using StreamVideo.Libs.Utils;
using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// SDK-owned local self-view texture. The instance stays the same until <see cref="Release"/>:
    /// it is resized in place to the camera size and refilled each frame from either the camera
    /// or the composited publisher frame.
    /// </summary>
    internal sealed class LocalPreviewRenderer : IDisposable
    {
        internal Texture LastSource { get; private set; }

        /// <summary>
        /// Returns the preview texture, creating it on first access. Main thread only.
        /// </summary>
        public RenderTexture GetOrCreate()
        {
            if (_texture != null)
            {
                return _texture;
            }

            _texture = new RenderTexture(PlaceholderSize, PlaceholderSize, 0, RenderTextureFormat.ARGB32)
            {
                name = "StreamLocalPreview",
            };
            _texture.Create();
            ClearToBlack(_texture);
            LastSource = null;
            return _texture;
        }

        /// <param name="camera">Local camera. Defines the preview size.</param>
        /// <param name="filtered">Composited publisher frame, or null to show the camera.</param>
        public void Render(Texture camera, Texture filtered)
        {
            if (_texture == null || camera == null || !IsReady(camera))
            {
                return;
            }

            var source = filtered != null ? filtered : camera;
            var resized = EnsureSize(camera.width, camera.height);

            if (!resized && source == LastSource && source is WebCamTexture webCam && !webCam.didUpdateThisFrame)
            {
                return;
            }

            Graphics.Blit(source, _texture);
            LastSource = source;
        }

        public void Release()
        {
            LastSource = null;
            if (_texture == null)
            {
                return;
            }

            if (RenderTexture.active == _texture)
            {
                RenderTexture.active = null;
            }

            _texture.Release();
            _texture.SmartDestroy();
            _texture = null;
        }

        public void Dispose() => Release();

        // WebCamTexture reports 16x16 until the device delivers its first frame
        private const int PlaceholderSize = 16;

        private RenderTexture _texture;

        private static bool IsReady(Texture camera)
            => !(camera is WebCamTexture) || (camera.width > PlaceholderSize && camera.height > PlaceholderSize);

        private bool EnsureSize(int width, int height)
        {
            if (_texture.width == width && _texture.height == height)
            {
                return false;
            }

            if (RenderTexture.active == _texture)
            {
                RenderTexture.active = null;
            }

            _texture.Release();
            _texture.width = width;
            _texture.height = height;
            _texture.Create();
            return true;
        }

        private static void ClearToBlack(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = previous;
        }
    }
}
