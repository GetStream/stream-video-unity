using System;
using Stream.Video.v1.Sfu.Models;

namespace StreamVideo.Core
{
    /// <summary>
    /// Video resolution
    ///
    /// Create a custom resolution or use one of the predefined:
    /// - <see cref="Res_1080p"/> FullHD -> 1920x1080
    /// - <see cref="Res_720p"/> HD -> 1280x720
    /// - <see cref="Res_480p"/> SD -> 640x480
    /// - <see cref="Res_360p"/> -> 480x360
    /// - <see cref="Res_240p"/> -> 320x240
    /// - <see cref="Res_144p"/> -> 256x144
    /// </summary>
    public struct VideoResolution
    {
        /// <summary>
        /// FullHD -> 1920x1080
        /// </summary>
        public static VideoResolution Res_1080p => new VideoResolution(1920, 1080);

        /// <summary>
        /// HD -> 1280x720
        /// </summary>
        public static VideoResolution Res_720p => new VideoResolution(1280, 720);

        /// <summary>
        /// SD -> 640x480
        /// </summary>
        public static VideoResolution Res_480p => new VideoResolution(640, 480);

        /// <summary>
        /// 480x360
        /// </summary>
        public static VideoResolution Res_360p => new VideoResolution(480, 360);

        /// <summary>
        /// 320x240
        /// </summary>
        public static VideoResolution Res_240p => new VideoResolution(320, 240);

        /// <summary>
        /// 256x144
        /// </summary>
        public static VideoResolution Res_144p => new VideoResolution(256, 144);

        public VideoResolution(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        public VideoResolution(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(width)} must be greater than 0. Passed value: {width}");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(height)} must be greater than 0. Passed value: {height}");
            }

            Width = (uint)width;
            Height = (uint)height;
        }

        public uint Width { get; }
        public uint Height { get; }

        public override string ToString() => $"Video resolution: {Width}x{Height}";

        internal VideoDimension ToVideoDimension() => new VideoDimension { Width = Width, Height = Height };
    }

    //StreamTodo: add reference to the docs for recommended video resolution
}