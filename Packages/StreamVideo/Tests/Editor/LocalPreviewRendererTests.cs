#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.BackgroundFilters;
using UnityEngine;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="LocalPreviewRenderer"/>.
    /// </summary>
    internal sealed class LocalPreviewRendererTests
    {
        [SetUp]
        public void SetUp()
        {
            _renderer = new LocalPreviewRenderer();
            _camera = CreateSource(64, 48);
            _published = CreatePublished(64, 48);
        }

        [TearDown]
        public void TearDown()
        {
            _renderer.Dispose();
            Object.DestroyImmediate(_camera);
            DestroyPublished(_published);
        }

        [Test]
        public void When_get_or_create_before_camera_expect_non_null_texture()
        {
            var texture = _renderer.GetOrCreate();

            Assert.That(texture, Is.Not.Null,
                "Preview must exist before the camera delivers frames so callers can bind it once.");
            Assert.That(_renderer.GetOrCreate(), Is.SameAs(texture),
                "Repeated reads must return the same instance.");
        }

        [Test]
        public void When_render_before_get_or_create_expect_noop()
        {
            _renderer.Render(_camera, null);

            Assert.That(_renderer.LastSource, Is.Null,
                "Renderer must not allocate or blit until something reads the preview.");
        }

        [Test]
        public void When_switching_between_camera_and_published_source_expect_same_instance()
        {
            var texture = _renderer.GetOrCreate();

            _renderer.Render(_camera, null);
            Assert.That(_renderer.LastSource, Is.SameAs(_camera),
                "Without a published frame the preview must show the camera.");

            _renderer.Render(_camera, _published);
            Assert.That(_renderer.LastSource, Is.SameAs(_published),
                "With a published frame the preview must show what other participants receive.");

            _renderer.Render(_camera, null);
            Assert.That(_renderer.LastSource, Is.SameAs(_camera),
                "Losing the published frame must fall back to the camera.");

            Assert.That(_renderer.GetOrCreate(), Is.SameAs(texture),
                "Source switches must not replace the preview instance.");
        }

        [Test]
        public void When_camera_size_changes_expect_same_instance_resized()
        {
            var texture = _renderer.GetOrCreate();
            _renderer.Render(_camera, null);
            Assert.That(texture.width, Is.EqualTo(_camera.width), "Preview width must match the camera.");
            Assert.That(texture.height, Is.EqualTo(_camera.height), "Preview height must match the camera.");

            var otherCamera = CreateSource(32, 24);
            try
            {
                _renderer.Render(otherCamera, null);

                Assert.That(_renderer.GetOrCreate(), Is.SameAs(texture),
                    "A camera switch must not replace the preview instance.");
                Assert.That(texture.width, Is.EqualTo(otherCamera.width), "Preview must resize in place.");
                Assert.That(texture.height, Is.EqualTo(otherCamera.height), "Preview must resize in place.");
            }
            finally
            {
                Object.DestroyImmediate(otherCamera);
            }
        }

        [Test]
        public void When_published_frame_is_upright_portrait_expect_same_instance_sized_to_published_frame()
        {
            var texture = _renderer.GetOrCreate();
            var portrait = CreatePublished(48, 64);
            try
            {
                _renderer.Render(_camera, portrait);

                Assert.That(_renderer.GetOrCreate(), Is.SameAs(texture),
                    "An orientation change must not replace the preview instance.");
                Assert.That(texture.width, Is.EqualTo(portrait.width),
                    "Preview width must match the upright published frame, not the camera sensor.");
                Assert.That(texture.height, Is.EqualTo(portrait.height),
                    "Preview height must match the upright published frame, not the camera sensor.");
            }
            finally
            {
                DestroyPublished(portrait);
            }
        }

        [Test]
        public void When_released_expect_texture_destroyed_and_next_read_creates_new_instance()
        {
            var texture = _renderer.GetOrCreate();

            _renderer.Release();

            Assert.That(texture == null, Is.True, "Release must destroy the preview texture.");
            var next = _renderer.GetOrCreate();
            Assert.That(next, Is.Not.Null, "A read after release must create a new preview.");
            Assert.That(ReferenceEquals(next, texture), Is.False, "The destroyed instance must not be reused.");
        }

        private static Texture2D CreateSource(int width, int height) => new Texture2D(width, height);

        private static RenderTexture CreatePublished(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.Create();
            return rt;
        }

        private static void DestroyPublished(RenderTexture rt)
        {
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        private LocalPreviewRenderer _renderer;
        private Texture2D _camera;
        private RenderTexture _published;
    }
}
#endif
