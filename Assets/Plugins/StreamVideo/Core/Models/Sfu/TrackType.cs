using System;
using System.Collections.Generic;
using System.Linq;
using TrackTypeInternalEnum = Stream.Video.v1.Sfu.Models.TrackType;
using OriginalNameAttr = Google.Protobuf.Reflection.OriginalNameAttribute;

namespace StreamVideo.Core.Models.Sfu
{
    internal enum TrackType
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

        public static bool TryGetTrackType(string trackTypeStr, out TrackType trackType)
            => TrackTypeMapping.TryGetValue(trackTypeStr.ToLower(), out trackType);

        static TrackTypeExt()
        {
            var type = typeof(TrackTypeInternalEnum);

            foreach (TrackTypeInternalEnum value in Enum.GetValues(typeof(TrackTypeInternalEnum)))
            {
                var memberInfos = type.GetMember(value.ToString());
                var valueMemberInfo = memberInfos.First(m => m.DeclaringType == type);
                var valueAttributes = valueMemberInfo.GetCustomAttributes(typeof(OriginalNameAttr), false);
                var name = ((OriginalNameAttr)valueAttributes[0]).Name;
                TrackTypeMapping.Add(name.ToLower(), value.ToPublicEnum());
            }
        }

        private static readonly Dictionary<string, TrackType> TrackTypeMapping = new Dictionary<string, TrackType>();
    }
}