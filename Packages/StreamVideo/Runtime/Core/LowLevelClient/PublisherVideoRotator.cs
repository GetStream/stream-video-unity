using System;
using StreamVideo.Core.BackgroundFilters;
using StreamVideo.Libs.Utils;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Rotates camera frames upright before they are encoded. The SFU does not negotiate the
    /// <c>urn:3gpp:video-orientation</c> RTP header extension, so every Stream SDK publishes upright pixels
    /// and receivers never apply rotation metadata.
    /// The camera is first copied with a default blit into a camera-space RT, the shader only runs between
    /// internal RTs, and the last hop into the WebRTC RT is a bit copy. Same constraints as
    /// <see cref="BackgroundCompositor"/>.
    /// </summary>
    internal sealed class PublisherVideoRotator : IDisposable
    {
        public const string ShaderName = "Hidden/StreamVideo/PublisherVideoRotation";
        public const string ShaderResourcesPath = "StreamVideo/PublisherVideoRotation";

        public bool IsSupported
        {
            get
            {
                EnsureMaterial();
                return _material != null;
            }
        }

        /// <summary>
        /// Camera-space frame written by the last <see cref="GetCameraSpaceTexture"/> caller, or null.
        /// </summary>
        public RenderTexture CameraSpaceTexture => _cameraSpaceRt;

        /// <summary>
        /// Clockwise rotation (0, 90, 180, 270) and vertical flip that make the camera image upright.
        /// Returns false while the camera is stopped or has not delivered its first frame.
        /// </summary>
        public static bool TryGetUprightOrientation(WebCamTexture camera, out int rotation, out bool verticallyMirrored)
        {
            if (!CameraOrientationDebug.CanReadWebCamOrientation(camera))
            {
                rotation = 0;
                verticallyMirrored = false;
                return false;
            }

            rotation = PersonMaskOrientation.NormalizeClockwiseDegrees(camera.videoRotationAngle);
            verticallyMirrored = camera.videoVerticallyMirrored;
            return true;
        }

        public static bool IsSideways(int rotation) => rotation == 90 || rotation == 270;

        public static VideoResolution Rotate(VideoResolution resolution, int rotation)
            => IsSideways(rotation) ? new VideoResolution(resolution.Height, resolution.Width) : resolution;

        /// <summary>
        /// 2x2 row-major matrix mapping destination UV to source UV around the texture center.
        /// The vertical flip is applied in camera space, before the rotation.
        /// </summary>
        public static Vector4 GetUvTransform(int rotation, bool verticallyMirrored = false)
        {
            Vector4 transform;
            switch (PersonMaskOrientation.NormalizeClockwiseDegrees(rotation))
            {
                case 90: transform = new Vector4(0f, -1f, 1f, 0f); break;
                case 180: transform = new Vector4(-1f, 0f, 0f, -1f); break;
                case 270: transform = new Vector4(0f, 1f, -1f, 0f); break;
                default: transform = new Vector4(1f, 0f, 0f, 1f); break;
            }

            if (verticallyMirrored)
            {
                transform.z = -transform.z;
                transform.w = -transform.w;
            }

            return transform;
        }

        /// <summary>
        /// Returns an RT with the unrotated size of <paramref name="destination"/> to write the camera frame into,
        /// or null if it could not be created.
        /// </summary>
        public RenderTexture GetCameraSpaceTexture(RenderTexture destination, int rotation)
        {
            var width = IsSideways(rotation) ? destination.height : destination.width;
            var height = IsSideways(rotation) ? destination.width : destination.height;
            _cameraSpaceRt = EnsureRt(_cameraSpaceRt, width, height, destination.format, "StreamPublisherCameraSpace");
            return _cameraSpaceRt;
        }

        /// <summary>
        /// Writes <paramref name="source"/> rotated clockwise by <paramref name="rotation"/> into
        /// <paramref name="destination"/>, flipping it vertically first if <paramref name="verticallyMirrored"/>.
        /// The destination must already have the rotated size.
        /// </summary>
        public bool TryRotate(Texture source, RenderTexture destination, int rotation, bool verticallyMirrored = false)
        {
            if (source == null || destination == null || !IsSupported)
            {
                return false;
            }

            _rotatedRt = EnsureRt(_rotatedRt, destination.width, destination.height, destination.format,
                "StreamPublisherRotated");
            if (_rotatedRt == null)
            {
                return false;
            }

            _material.SetVector(UvTransformId, GetUvTransform(rotation, verticallyMirrored));
            Graphics.Blit(source, _rotatedRt, _material);

            if (BackgroundCompositor.CanCopyTexture(_rotatedRt, destination))
            {
                Graphics.CopyTexture(_rotatedRt, destination);
            }
            else
            {
                Graphics.Blit(_rotatedRt, destination);
            }

            return true;
        }

        public void Dispose()
        {
            ReleaseRt(ref _cameraSpaceRt);
            ReleaseRt(ref _rotatedRt);
            if (_material != null)
            {
                _material.SmartDestroy();
                _material = null;
            }
        }

        private static readonly int UvTransformId = Shader.PropertyToID("_UvTransform");

        private Material _material;
        private bool _materialLoadAttempted;
        private RenderTexture _cameraSpaceRt;
        private RenderTexture _rotatedRt;

        private void EnsureMaterial()
        {
            if (_material != null || _materialLoadAttempted)
            {
                return;
            }

            _materialLoadAttempted = true;
            _material = BackgroundCompositor.CreateMaterial(ShaderName, ShaderResourcesPath);
        }

        private static RenderTexture EnsureRt(RenderTexture current, int width, int height,
            RenderTextureFormat format, string name)
        {
            if (current != null && current.IsCreated() && current.width == width && current.height == height
                && current.format == format)
            {
                return current;
            }

            ReleaseRt(ref current);

            var rt = new RenderTexture(width, height, 0, format)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            rt.Create();
            if (rt.IsCreated())
            {
                return rt;
            }

            ReleaseRt(ref rt);
            return null;
        }

        private static void ReleaseRt(ref RenderTexture rt)
        {
            if (rt == null)
            {
                return;
            }

            if (RenderTexture.active == rt)
            {
                RenderTexture.active = null;
            }

            rt.Release();
            rt.SmartDestroy();
            rt = null;
        }
    }
}
