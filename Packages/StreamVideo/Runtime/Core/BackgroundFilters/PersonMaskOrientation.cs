using System;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Clockwise pixel-buffer rotation for ML Kit input and inverse mapping of the mask
    /// back to <c>WebCamTexture</c> UV space. Uses row-major layout with y increasing downward
    /// (Android Bitmap / packed RGBA from GPU readback as currently uploaded).
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
