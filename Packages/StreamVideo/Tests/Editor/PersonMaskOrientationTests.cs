#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.BackgroundFilters;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="PersonMaskOrientation"/>.
    /// </summary>
    internal sealed class PersonMaskOrientationTests
    {
        [Test]
        public void When_mask_rotated_90_then_unrotated_expect_original_bytes()
        {
            // 2x3 fixture, unique values so a no-op rotate cannot pass the round-trip.
            var original = new byte[] { 10, 20, 30, 40, 50, 60 };
            const int width = 2;
            const int height = 3;

            var rotated = PersonMaskOrientation.RotateClockwise(original, width, height, 1, 90,
                out var rotatedWidth, out var rotatedHeight);

            Assert.That(rotatedWidth, Is.EqualTo(3),
                "Clockwise 90 must swap width and height.");
            Assert.That(rotatedHeight, Is.EqualTo(2),
                "Clockwise 90 must swap width and height.");
            Assert.That(rotated, Is.EqualTo(new byte[] { 50, 30, 10, 60, 40, 20 }),
                "Clockwise 90 of a 2x3 R8 buffer must match row-major y-down mapping.");

            var restored = PersonMaskOrientation.UnrotateClockwise(rotated, rotatedWidth, rotatedHeight, 1, 90,
                out var restoredWidth, out var restoredHeight);

            Assert.That(restoredWidth, Is.EqualTo(width),
                "Unrotate must restore the original mask width for compositor UVs.");
            Assert.That(restoredHeight, Is.EqualTo(height),
                "Unrotate must restore the original mask height for compositor UVs.");
            Assert.That(restored, Is.EqualTo(original),
                "Rotate 90 then unrotate must yield the original webcam-space mask bytes.");
        }

        [Test]
        public void When_mask_rotated_180_then_unrotated_expect_original_bytes()
        {
            var original = new byte[] { 10, 20, 30, 40, 50, 60 };
            var rotated = PersonMaskOrientation.RotateClockwise(original, 2, 3, 1, 180,
                out var rotatedWidth, out var rotatedHeight);

            Assert.That(rotatedWidth, Is.EqualTo(2),
                "Clockwise 180 must keep width.");
            Assert.That(rotatedHeight, Is.EqualTo(3),
                "Clockwise 180 must keep height.");

            var restored = PersonMaskOrientation.UnrotateClockwise(rotated, rotatedWidth, rotatedHeight, 1, 180,
                out var restoredWidth, out var restoredHeight);

            Assert.That(restoredWidth, Is.EqualTo(2),
                "Unrotate 180 must restore original width.");
            Assert.That(restoredHeight, Is.EqualTo(3),
                "Unrotate 180 must restore original height.");
            Assert.That(restored, Is.EqualTo(original),
                "Rotate 180 then unrotate must yield the original webcam-space mask bytes.");
        }

        [Test]
        public void When_mask_flipped_vertically_expect_rows_reversed()
        {
            var original = new byte[] { 10, 20, 30, 40, 50, 60 };
            PersonMaskOrientation.FlipVertical(original, 2, 3, 1);

            Assert.That(original, Is.EqualTo(new byte[] { 50, 60, 30, 40, 10, 20 }),
                "Vertical flip must reverse row order without changing width.");

            PersonMaskOrientation.FlipVertical(original, 2, 3, 1);
            Assert.That(original, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }),
                "Flipping twice must restore the original bytes.");
        }

        [Test]
        public void When_async_readback_on_gles_expect_no_y_flip()
        {
            Assert.That(PersonMaskOrientation.NeedsAsyncGpuReadbackYFlip(false, true), Is.False,
                "GLES bottom-up readback already matches Bitmap/compositor layout.");
            Assert.That(PersonMaskOrientation.NeedsAsyncGpuReadbackYFlip(true, true), Is.False,
                "Never Y-flip GLES even if graphicsUVStartsAtTop is reported.");
        }

        [Test]
        public void When_async_readback_on_vulkan_or_metal_expect_y_flip()
        {
            Assert.That(PersonMaskOrientation.NeedsAsyncGpuReadbackYFlip(true, false), Is.True,
                "Vulkan and Metal top-origin AsyncGPUReadback must be flipped once into y-down layout.");
            Assert.That(PersonMaskOrientation.NeedsAsyncGpuReadbackYFlip(false, false), Is.False,
                "Do not flip when the GPU origin already matches y-down.");
        }

        [Test]
        public void When_uploading_bitmap_layout_mask_expect_texture2d_flip_on_metal_only()
        {
            Assert.That(PersonMaskOrientation.NeedsYFlipFromBitmapLayoutToTexture2D(true), Is.False,
                "GLES OES WebCamTexture already matches Bitmap y-down bytes in a Texture2D.");
            Assert.That(PersonMaskOrientation.NeedsYFlipFromBitmapLayoutToTexture2D(false), Is.True,
                "iOS Metal WebCamTexture is a regular 2D texture; flip Bitmap-layout mask bytes on upload.");
        }
    }
}
#endif
