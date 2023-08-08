using System;
using TrackTypeInternalEnum = Stream.Video.v1.Sfu.Models.TrackType;

namespace StreamVideo.Core.Models.Sfu
{
    public enum TrackType
    {
        Unspecified = 0,
        Audio = 1,
        Video = 2,
        ScreenShare = 3,
        ScreenShareAudio = 4,
    }

    internal static class TrackTypeExt
    {
        public static TrackTypeInternalEnum ToInternalEnum(this TrackType trackType)
        {
            switch (trackType)
            {
                case TrackType.Unspecified: return TrackTypeInternalEnum.Unspecified;
                case TrackType.Audio: return TrackTypeInternalEnum.Audio;
                case TrackType.Video: return TrackTypeInternalEnum.Video;
                case TrackType.ScreenShare: return TrackTypeInternalEnum.ScreenShare;
                case TrackType.ScreenShareAudio: return TrackTypeInternalEnum.ScreenShareAudio;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null);
            }
        }

        public static TrackType ToPublicEnum(this TrackTypeInternalEnum trackType)
        {
            switch (trackType)
            {
                case TrackTypeInternalEnum.Unspecified: return TrackType.Unspecified;
                case TrackTypeInternalEnum.Audio: return TrackType.Audio;
                case TrackTypeInternalEnum.Video: return TrackType.Video;
                case TrackTypeInternalEnum.ScreenShare: return TrackType.ScreenShare;
                case TrackTypeInternalEnum.ScreenShareAudio: return TrackType.ScreenShareAudio;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null);
            }
        }
    }
}