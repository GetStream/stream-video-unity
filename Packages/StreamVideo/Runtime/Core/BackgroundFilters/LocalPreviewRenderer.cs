using System;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Libs.Utils;
using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// SDK-owned local self-view texture. The instance stays the same until <see cref="Release"/>:
    /// it is resized in place and refilled each frame with the upright image, either the published frame
    /// or, while nothing is published, the camera rotated by its <see cref="WebCamTexture.videoRotationAngle"/>.
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

        /// <param name="camera">Local camera, shown rotated upright while <paramref name="published"/> is null.</param>
        /// <param name="published">Upright frame sent to other participants, or null while nothing is published.</param>
        public void Render(Texture camera, RenderTexture published)
        {
            if (_texture == null || camera == null || !IsReady(camera))
            {
                return;
            }

            if (published != null)
            {
                EnsureSize(published.width, published.height);
                if (BackgroundCompositor.CanCopyTexture(published, _texture))
                {
                    Graphics.CopyTexture(published, _texture);
                }
                else
                {
                    Graphics.Blit(published, _texture);
                }

                LastSource = published;
                return;
            }

            RenderCamera(camera);
        }

        public void Release()
        {
            LastSource = null;
            _rotator?.Dispose();
            _rotator = null;
            _cameraRotation = 0;
            _cameraVerticallyMirrored = false;

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
        private PublisherVideoRotator _rotator;
        // Last known camera orientation, kept while the camera is stopped
        private int _cameraRotation;
        private bool _cameraVerticallyMirrored;

        private static bool IsReady(Texture camera)
            => !(camera is WebCamTexture) || (camera.width > PlaceholderSize && camera.height > PlaceholderSize);

        private void RenderCamera(Texture camera)
        {
            var webCam = camera as WebCamTexture;
            UpdateCameraOrientation(webCam);

            var size = PublisherVideoRotator.Rotate(new VideoResolution(camera.width, camera.height), _cameraRotation);
            var resized = EnsureSize((int)size.Width, (int)size.Height);

            if (!resized && camera == LastSource && webCam != null && !webCam.didUpdateThisFrame)
            {
                return;
            }

            if (_cameraRotation == 0 && !_cameraVerticallyMirrored)
            {
                Graphics.Blit(camera, _texture);
            }
            else
            {
                // The rotation shader must not sample the WebCamTexture directly, see BackgroundCompositor
                var cameraSpace = _rotator.GetCameraSpaceTexture(_texture, _cameraRotation);
                if (cameraSpace == null)
                {
                    return;
                }

                Graphics.Blit(camera, cameraSpace);
                _rotator.TryRotate(cameraSpace, _texture, _cameraRotation, _cameraVerticallyMirrored);
            }

            LastSource = camera;
        }

        private void UpdateCameraOrientation(WebCamTexture webCam)
        {
            if (webCam == null)
            {
                _cameraRotation = 0;
                _cameraVerticallyMirrored = false;
                return;
            }

            if (!PublisherVideoRotator.TryGetUprightOrientation(webCam, out var rotation, out var verticallyMirrored))
            {
                return;
            }

            if (rotation != 0 || verticallyMirrored)
            {
                _rotator = _rotator ?? new PublisherVideoRotator();
                if (!_rotator.IsSupported)
                {
                    rotation = 0;
                    verticallyMirrored = false;
                }
            }

            _cameraRotation = rotation;
            _cameraVerticallyMirrored = verticallyMirrored;
        }

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
