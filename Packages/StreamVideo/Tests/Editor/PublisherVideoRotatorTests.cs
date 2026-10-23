#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.LowLevelClient;
using UnityEngine;
using UnityEngine.Rendering;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="PublisherVideoRotator"/>.
    /// </summary>
    internal sealed class PublisherVideoRotatorTests
    {
        [TearDown]
        public void TearDown()
        {
            _rotator?.Dispose();
            _rotator = null;

            if (_destination != null)
            {
                _destination.Release();
                Object.DestroyImmediate(_destination);
                _destination = null;
            }

            if (_source != null)
            {
                Object.DestroyImmediate(_source);
                _source = null;
            }
        }

        [TestCase(90)]
        [TestCase(270)]
        public void When_rotation_is_sideways_expect_resolution_swapped(int rotation)
        {
            var rotated = PublisherVideoRotator.Rotate(new VideoResolution(1280, 720), rotation);

            Assert.That(rotated.Width, Is.EqualTo(720u), "Sideways rotation must publish the camera height as width.");
            Assert.That(rotated.Height, Is.EqualTo(1280u),
                "Sideways rotation must publish the camera width as height.");
        }

        [TestCase(0)]
        [TestCase(180)]
        public void When_rotation_is_not_sideways_expect_resolution_unchanged(int rotation)
        {
            var rotated = PublisherVideoRotator.Rotate(new VideoResolution(1280, 720), rotation);

            Assert.That(rotated.Width, Is.EqualTo(1280u), "0 and 180 degree rotations must keep the width.");
            Assert.That(rotated.Height, Is.EqualTo(720u), "0 and 180 degree rotations must keep the height.");
        }

        [TestCase(0, 0f, 1f, 0f, 1f)]
        [TestCase(90, 0f, 1f, 0f, 0f)]
        [TestCase(180, 0f, 1f, 1f, 0f)]
        [TestCase(270, 0f, 1f, 1f, 1f)]
        [TestCase(-90, 0f, 1f, 1f, 1f)]
        public void When_uv_transform_maps_destination_uv_expect_clockwise_source_uv(int rotation, float destU,
            float destV, float expectedSourceU, float expectedSourceV)
        {
            var source = MapUv(PublisherVideoRotator.GetUvTransform(rotation), new Vector2(destU, destV));

            Assert.That(source.x, Is.EqualTo(expectedSourceU).Within(0.0001f),
                $"Destination top-left must sample the expected source U for {rotation} degrees clockwise.");
            Assert.That(source.y, Is.EqualTo(expectedSourceV).Within(0.0001f),
                $"Destination top-left must sample the expected source V for {rotation} degrees clockwise.");
        }

        [TestCase(0, 0f, 1f, 0f, 0f)]
        [TestCase(90, 0f, 1f, 0f, 1f)]
        [TestCase(270, 0f, 1f, 1f, 0f)]
        public void When_uv_transform_is_vertically_mirrored_expect_source_v_flipped_before_rotation(int rotation,
            float destU, float destV, float expectedSourceU, float expectedSourceV)
        {
            var source = MapUv(PublisherVideoRotator.GetUvTransform(rotation, verticallyMirrored: true),
                new Vector2(destU, destV));

            Assert.That(source.x, Is.EqualTo(expectedSourceU).Within(0.0001f),
                $"Mirroring must not change the source U for {rotation} degrees clockwise.");
            Assert.That(source.y, Is.EqualTo(expectedSourceV).Within(0.0001f),
                $"Mirroring must flip the source V in camera space for {rotation} degrees clockwise.");
        }

        [Test]
        public void When_vertically_mirrored_without_rotation_expect_bottom_and_top_swapped()
        {
            IgnoreIfRotationUnsupported();

            _rotator = new PublisherVideoRotator();
            _source = new Texture2D(1, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _source.SetPixels(new[] { Color.red, Color.green });
            _source.Apply();
            _destination = CreateRt(1, 2);

            Assert.That(_rotator.TryRotate(_source, _destination, 0, verticallyMirrored: true), Is.True,
                "Mirroring must succeed when the shader is supported.");
            Assert.That(ReadPixel(0, 0), Is.EqualTo(Color.green), "Mirroring must move the top row to the bottom.");
            Assert.That(ReadPixel(0, 1), Is.EqualTo(Color.red), "Mirroring must move the bottom row to the top.");
        }

        [Test]
        public void When_rotating_90_clockwise_expect_left_edge_on_top()
        {
            RotateLeftRedRightGreen(90, 1, 2);

            Assert.That(ReadPixel(0, 1), Is.EqualTo(Color.red), "Rotating 90 clockwise must move the left edge to the top.");
            Assert.That(ReadPixel(0, 0), Is.EqualTo(Color.green),
                "Rotating 90 clockwise must move the right edge to the bottom.");
        }

        [Test]
        public void When_rotating_270_clockwise_expect_left_edge_on_bottom()
        {
            RotateLeftRedRightGreen(270, 1, 2);

            Assert.That(ReadPixel(0, 0), Is.EqualTo(Color.red),
                "Rotating 270 clockwise must move the left edge to the bottom.");
            Assert.That(ReadPixel(0, 1), Is.EqualTo(Color.green),
                "Rotating 270 clockwise must move the right edge to the top.");
        }

        [Test]
        public void When_rotating_180_expect_left_and_right_swapped()
        {
            RotateLeftRedRightGreen(180, 2, 1);

            Assert.That(ReadPixel(0, 0), Is.EqualTo(Color.green), "Rotating 180 must move the right edge to the left.");
            Assert.That(ReadPixel(1, 0), Is.EqualTo(Color.red), "Rotating 180 must move the left edge to the right.");
        }

        [Test]
        public void When_destination_is_sideways_expect_camera_space_texture_has_unrotated_size()
        {
            IgnoreIfRotationUnsupported();

            _rotator = new PublisherVideoRotator();
            _destination = CreateRt(720, 1280);

            var cameraSpace = _rotator.GetCameraSpaceTexture(_destination, 90);

            Assert.That(cameraSpace, Is.Not.Null, "Camera-space RT must be created.");
            Assert.That(cameraSpace.width, Is.EqualTo(1280), "Camera-space RT must keep the camera width.");
            Assert.That(cameraSpace.height, Is.EqualTo(720), "Camera-space RT must keep the camera height.");
        }

        private PublisherVideoRotator _rotator;
        private RenderTexture _destination;
        private Texture2D _source;

        private static Vector2 MapUv(Vector4 transform, Vector2 uv)
        {
            var centered = uv - new Vector2(0.5f, 0.5f);
            return new Vector2(
                transform.x * centered.x + transform.y * centered.y + 0.5f,
                transform.z * centered.x + transform.w * centered.y + 0.5f);
        }

        private void RotateLeftRedRightGreen(int rotation, int destinationWidth, int destinationHeight)
        {
            IgnoreIfRotationUnsupported();

            _rotator = new PublisherVideoRotator();
            _source = new Texture2D(2, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _source.SetPixels(new[] { Color.red, Color.green });
            _source.Apply();
            _destination = CreateRt(destinationWidth, destinationHeight);

            Assert.That(_rotator.TryRotate(_source, _destination, rotation), Is.True,
                "Rotation must succeed when the shader is supported.");
        }

        private Color ReadPixel(int x, int y)
        {
            var previous = RenderTexture.active;
            var readback = new Texture2D(_destination.width, _destination.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = _destination;
                readback.ReadPixels(new Rect(0, 0, _destination.width, _destination.height), 0, 0);
                readback.Apply();
                return readback.GetPixel(x, y);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
            }
        }

        private static RenderTexture CreateRt(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.Create();
            return rt;
        }

        private static void IgnoreIfRotationUnsupported()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Rotation needs a GPU; the Editor is running without graphics.");
            }

            var shader = Shader.Find(PublisherVideoRotator.ShaderName)
                ?? Resources.Load<Shader>(PublisherVideoRotator.ShaderResourcesPath);
            if (shader == null || !shader.isSupported)
            {
                Assert.Ignore("Publisher video rotation shader is not available on this Editor GPU.");
            }
        }
    }
}
#endif
