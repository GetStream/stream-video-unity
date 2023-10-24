using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum RecordSettingsQuality
    {
        AudioOnly = 0,
        _360p = 1,
        _480p = 2,
        _720p = 3,
        _1080p = 4,
        _1440p = 5,
    }

    internal static class RecordSettingsQualityInternalEnumExt
    {
        public static RecordSettingsQuality ToPublicEnum(this RecordSettingsQualityInternalEnum internalValue)
        {
            switch (internalValue)
            {
                case RecordSettingsQualityInternalEnum.AudioOnly: return RecordSettingsQuality.AudioOnly;
                case RecordSettingsQualityInternalEnum._360p: return RecordSettingsQuality._360p;
                case RecordSettingsQualityInternalEnum._480p: return RecordSettingsQuality._480p;
                case RecordSettingsQualityInternalEnum._720p: return RecordSettingsQuality._720p;
                case RecordSettingsQualityInternalEnum._1080p: return RecordSettingsQuality._1080p;
                case RecordSettingsQualityInternalEnum._1440p: return RecordSettingsQuality._1440p;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, null);
            }
        }

        public static RecordSettingsQualityInternalEnum ToInternalEnum(this RecordSettingsQuality publicValue)
        {
            switch (publicValue)
            {
                case RecordSettingsQuality.AudioOnly: return RecordSettingsQualityInternalEnum.AudioOnly;
                case RecordSettingsQuality._360p: return RecordSettingsQualityInternalEnum._360p;
                case RecordSettingsQuality._480p: return RecordSettingsQualityInternalEnum._480p;
                case RecordSettingsQuality._720p: return RecordSettingsQualityInternalEnum._720p;
                case RecordSettingsQuality._1080p: return RecordSettingsQualityInternalEnum._1080p;
                case RecordSettingsQuality._1440p: return RecordSettingsQualityInternalEnum._1440p;
                default:
                    throw new ArgumentOutOfRangeException(nameof(publicValue), publicValue, null);
            }
        }
        
        public static RecordSettingsQuality ParseToPublicEnum(string internalValue)
        {
            switch (internalValue)
            {
                case "audio-only": return RecordSettingsQuality.AudioOnly;
                case "360p": return RecordSettingsQuality._360p;
                case "480p": return RecordSettingsQuality._480p;
                case "720p": return RecordSettingsQuality._720p;
                case "1080p": return RecordSettingsQuality._1080p;
                case "1440p": return RecordSettingsQuality._1440p;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, null);
            }
        }
        
    }
}