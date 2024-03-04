using System;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using SfuTrackType = Stream.Video.v1.Sfu.Models.TrackType;
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
        public static SfuTrackType ToInternalEnum(this TrackKind trackType)
        {
            switch (trackType)
            {
                case TrackKind.Audio: return SfuTrackType.Audio;
                case TrackKind.Video: return SfuTrackType.Video;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null);
            }
        }
        
        public static SfuTrackType ToInternalEnum(this TrackType trackType)
        {
            switch (trackType)
            {
                case TrackType.Unspecified: return SfuTrackType.Unspecified;
                case TrackType.Audio: return SfuTrackType.Audio;
                case TrackType.Video: return SfuTrackType.Video;
                case TrackType.ScreenShare: return SfuTrackType.ScreenShare;
                case TrackType.ScreenShareAudio: return SfuTrackType.ScreenShareAudio;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null);
            }
        }

        public static TrackType ToPublicEnum(this SfuTrackType trackType)
        {
            switch (trackType)
            {
                case SfuTrackType.Unspecified: return TrackType.Unspecified;
                case SfuTrackType.Audio: return TrackType.Audio;
                case SfuTrackType.Video: return TrackType.Video;
                case SfuTrackType.ScreenShare: return TrackType.ScreenShare;
                case SfuTrackType.ScreenShareAudio: return TrackType.ScreenShareAudio;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null);
            }
        }

        public static bool TryGetTrackType(string trackTypeStr, out TrackType trackType)
            => TrackTypeMapping.TryGetValue(trackTypeStr.ToLower(), out trackType);

        static TrackTypeExt()
        {
            var type = typeof(SfuTrackType);

            foreach (SfuTrackType value in Enum.GetValues(typeof(SfuTrackType)))
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