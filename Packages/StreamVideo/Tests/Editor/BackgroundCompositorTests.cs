#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.BackgroundFilters;
using UnityEngine;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="BackgroundCompositor"/>.
    /// </summary>
    internal sealed class BackgroundCompositorTests
    {
        [TearDown]
        public void TearDown()
        {
            _compositor?.Release();
            _compositor = null;

            if (_destination != null)
            {
                _destination.Release();
                UnityEngine.Object.DestroyImmediate(_destination);
                _destination = null;
            }
        }

        [Test]
        public void When_r8_unsupported_expect_mask_format_is_argb32()
        {
            Assert.That(BackgroundCompositor.ChooseMaskRtFormat(false), Is.EqualTo(RenderTextureFormat.ARGB32),
                "Mask RTs must fall back to ARGB32 when R8 is unavailable.");
        }

        [Test]
        public void When_r8_supported_expect_mask_format_is_r8()
        {
            Assert.That(BackgroundCompositor.ChooseMaskRtFormat(true), Is.EqualTo(RenderTextureFormat.R8),
                "Mask RTs must keep linear R8 when the GPU supports it.");
        }

        [Test]
        public void When_system_r8_support_matches_expect_default_mask_format()
        {
            var expected = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;

            Assert.That(BackgroundCompositor.ChooseMaskRtFormat(), Is.EqualTo(expected),
                "Default mask format must stay linear R8 when supported and ARGB32 otherwise.");
        }

        [Test]
        public void When_shader_missing_expect_create_material_returns_null()
        {
            var material = BackgroundCompositor.CreateMaterial("Hidden/StreamVideo/DoesNotExist",
                "StreamVideo/BackgroundFilters/DoesNotExist");

            Assert.That(material, Is.Null,
                "Missing or unsupported shaders must not create a material so Apply can passthrough.");
        }

        [Test]
        public void When_render_texture_is_null_expect_is_usable_is_false()
        {
            Assert.That(BackgroundCompositor.IsUsable(null), Is.False,
                "A missing mask RT must be treated as unusable so Apply passthroughs.");
        }

        [Test]
        public void When_r8_forced_unavailable_expect_apply_does_not_passthrough()
        {
            IgnoreIfBackgroundFilterShadersUnsupported();

            _compositor = new BackgroundCompositor
            {
                MaskRtFormatOverride = RenderTextureFormat.ARGB32,
            };
            _destination = CreateDestination();

            _compositor.Apply(Texture2D.whiteTexture, _destination);

            Assert.That(_compositor.IsReady, Is.True,
                "ARGB32 Linear mask RTs must still allow compositing when R8 is unavailable.");
            Assert.That(_compositor.LastApplyWasPassthrough, Is.False,
                "R8 fallback must composite, not passthrough, when shaders and ARGB32 RTs work.");
        }

        [Test]
        public void When_default_mask_format_expect_apply_does_not_passthrough()
        {
            IgnoreIfBackgroundFilterShadersUnsupported();

            _compositor = new BackgroundCompositor();
            _destination = CreateDestination();

            _compositor.Apply(Texture2D.whiteTexture, _destination);

            Assert.That(_compositor.IsReady, Is.True,
                "Default mask format (linear R8 when supported) must create usable RTs.");
            Assert.That(_compositor.LastApplyWasPassthrough, Is.False,
                "Supported GPU must composite rather than passthrough.");
        }

        [Test]
        public void When_apply_expect_destination_size_is_unchanged()
        {
            IgnoreIfBackgroundFilterShadersUnsupported();

            _compositor = new BackgroundCompositor();
            _destination = CreateDestination();
            const int width = 16;
            const int height = 16;

            _compositor.Apply(Texture2D.whiteTexture, _destination);

            Assert.That(_destination.width, Is.EqualTo(width),
                "Composite must keep publisher RT width so receivers still apply videoRotationAngle.");
            Assert.That(_destination.height, Is.EqualTo(height),
                "Composite must keep publisher RT height so receivers still apply videoRotationAngle.");
            Assert.That(_compositor.LastApplyWasPassthrough, Is.False,
                "Size-preserving apply must still composite.");
        }

        [Test]
        public void When_copy_args_missing_expect_can_copy_texture_is_false()
        {
            Assert.That(BackgroundCompositor.CanCopyTexture(null, null), Is.False,
                "CopyTexture requires both RTs.");

            _destination = CreateDestination();
            Assert.That(BackgroundCompositor.CanCopyTexture(null, _destination), Is.False,
                "CopyTexture requires a source RT.");
            Assert.That(BackgroundCompositor.CanCopyTexture(_destination, null), Is.False,
                "CopyTexture requires a destination RT.");
        }

        [Test]
        public void When_copy_args_not_created_expect_can_copy_texture_is_false()
        {
            _destination = CreateDestination();
            var released = CreateDestination();
            released.Release();

            Assert.That(BackgroundCompositor.CanCopyTexture(released, _destination), Is.False,
                "CopyTexture must not run on a released source so Apply can blit after GPU context loss.");
            Assert.That(BackgroundCompositor.CanCopyTexture(_destination, released), Is.False,
                "CopyTexture must not run on a released destination so Apply can blit after GPU context loss.");

            UnityEngine.Object.DestroyImmediate(released);
        }

        private static void IgnoreIfBackgroundFilterShadersUnsupported()
        {
            var shader = Shader.Find("Hidden/StreamVideo/BackgroundMaskBlend")
                ?? Resources.Load<Shader>("StreamVideo/BackgroundFilters/BackgroundMaskBlend");
            if (shader == null || !shader.isSupported)
            {
                Assert.Ignore("Background filter shaders are not available on this Editor GPU.");
            }
        }

        private static RenderTexture CreateDestination()
        {
            var destination = new RenderTexture(16, 16, 0, RenderTextureFormat.ARGB32);
            destination.Create();
            return destination;
        }

        private BackgroundCompositor _compositor;
        private RenderTexture _destination;
    }
}
#endif
