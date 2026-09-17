using System;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Clockwise pixel-buffer rotation for native segmenter input and inverse mapping of the
    /// mask back to <c>WebCamTexture</c> UV space. Uses row-major layout with y increasing
    /// downward (Android Bitmap / GLES GPU readback). Vulkan and Metal AsyncGPUReadback are
    /// flipped into this layout once. iOS Metal flips again on Texture2D upload so the mask
    /// matches a regular 2D <c>WebCamTexture</c>; Android GLES does not.
    /// </summary>
    internal static class PersonMaskOrientation
    {
        public static int NormalizeClockwiseDegrees(int degrees)
        {
            var n = degrees % 360;
            if (n < 0)
            {
                n += 360;
            }

            return n / 90 * 90 % 360;
        }

        public static void GetRotatedSize(int width, int height, int degreesClockwise, out int destWidth,
            out int destHeight)
        {
            var degrees = NormalizeClockwiseDegrees(degreesClockwise);
            if (degrees == 90 || degrees == 270)
            {
                destWidth = height;
                destHeight = width;
                return;
            }

            destWidth = width;
            destHeight = height;
        }

        public static byte[] RotateClockwise(byte[] source, int width, int height, int bytesPerPixel,
            int degreesClockwise, out int destWidth, out int destHeight)
        {
            if (source == null)
            {
                destWidth = 0;
                destHeight = 0;
                return null;
            }

            var degrees = NormalizeClockwiseDegrees(degreesClockwise);
            GetRotatedSize(width, height, degrees, out destWidth, out destHeight);
            if (degrees == 0)
            {
                return source;
            }

            var dest = new byte[destWidth * destHeight * bytesPerPixel];
            RotateClockwise(source, width, height, bytesPerPixel, degrees, dest);
            return dest;
        }

        public static byte[] UnrotateClockwise(byte[] source, int width, int height, int bytesPerPixel,
            int degreesClockwise, out int destWidth, out int destHeight)
        {
            var inverse = (360 - NormalizeClockwiseDegrees(degreesClockwise)) % 360;
            return RotateClockwise(source, width, height, bytesPerPixel, inverse, out destWidth, out destHeight);
        }

        public static void RotateClockwise(byte[] source, int width, int height, int bytesPerPixel,
            int degreesClockwise, byte[] dest)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            if (width <= 0 || height <= 0 || bytesPerPixel <= 0)
            {
                throw new ArgumentException("width, height, and bytesPerPixel must be positive.");
            }

            var degrees = NormalizeClockwiseDegrees(degreesClockwise);
            GetRotatedSize(width, height, degrees, out var destWidth, out var destHeight);
            var srcNeeded = width * height * bytesPerPixel;
            var destNeeded = destWidth * destHeight * bytesPerPixel;
            if (source.Length < srcNeeded)
            {
                throw new ArgumentException("Source buffer is smaller than width*height*bytesPerPixel.",
                    nameof(source));
            }

            if (dest.Length < destNeeded)
            {
                throw new ArgumentException("Destination buffer is smaller than the rotated size.", nameof(dest));
            }

            if (degrees == 0)
            {
                Buffer.BlockCopy(source, 0, dest, 0, srcNeeded);
                return;
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    MapClockwise(x, y, width, height, degrees, out var destX, out var destY);
                    var srcIndex = (y * width + x) * bytesPerPixel;
                    var destIndex = (destY * destWidth + destX) * bytesPerPixel;
                    Buffer.BlockCopy(source, srcIndex, dest, destIndex, bytesPerPixel);
                }
            }
        }

        /// <summary>
        /// GLES AsyncGPUReadback matches this y-down layout with no CPU flip. Vulkan and
        /// Metal (graphicsUVStartsAtTop) are the opposite; flip once after readback.
        /// ReadPixels is already Unity Texture2D bottom-up and must not use this path.
        /// iOS Metal still needs <see cref="NeedsYFlipFromBitmapLayoutToTexture2D"/> on
        /// mask upload: a regular 2D WebCamTexture has UV y=0 at the bottom, unlike GLES OES.
        /// </summary>
        public static bool NeedsAsyncGpuReadbackYFlip(bool graphicsUvStartsAtTop, bool isOpenGles)
        {
            if (isOpenGles)
            {
                return false;
            }

            return graphicsUvStartsAtTop;
        }

        /// <summary>
        /// Bitmap y-down row 0 is the top of the image. <c>Texture2D.SetPixelData</c> row 0
        /// is the bottom. GLES OES <c>WebCamTexture</c> sampling matches y-down bytes in a
        /// Texture2D, so Android does not flip on upload. iOS Metal <c>WebCamTexture</c> is a
        /// regular 2D texture (UV y=0 = bottom), so the mask must be flipped on upload.
        /// Without that, a vertical mismatch in sensor space shows up as a left/right shift
        /// after a 90° portrait preview rotation and only lines up when the head is centered.
        /// </summary>
        public static bool NeedsYFlipFromBitmapLayoutToTexture2D(bool isOpenGles) => !isOpenGles;

        public static void FlipVertical(byte[] buffer, int width, int height, int bytesPerPixel)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (width <= 0 || height <= 0 || bytesPerPixel <= 0)
            {
                throw new ArgumentException("width, height, and bytesPerPixel must be positive.");
            }

            var rowBytes = width * bytesPerPixel;
            var needed = rowBytes * height;
            if (buffer.Length < needed)
            {
                throw new ArgumentException("Buffer is smaller than width*height*bytesPerPixel.", nameof(buffer));
            }

            if (height < 2)
            {
                return;
            }

            var temp = new byte[rowBytes];
            for (var y = 0; y < height / 2; y++)
            {
                var top = y * rowBytes;
                var bottom = (height - 1 - y) * rowBytes;
                Buffer.BlockCopy(buffer, top, temp, 0, rowBytes);
                Buffer.BlockCopy(buffer, bottom, buffer, top, rowBytes);
                Buffer.BlockCopy(temp, 0, buffer, bottom, rowBytes);
            }
        }

        private static void MapClockwise(int x, int y, int width, int height, int degrees, out int destX,
            out int destY)
        {
            switch (degrees)
            {
                case 90:
                    destX = height - 1 - y;
                    destY = x;
                    return;
                case 180:
                    destX = width - 1 - x;
                    destY = height - 1 - y;
                    return;
                case 270:
                    destX = y;
                    destY = width - 1 - x;
                    return;
                default:
                    destX = x;
                    destY = y;
                    return;
            }
        }
    }
}
