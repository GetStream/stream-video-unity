using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum RecordSettingsQuality
    {
        _360p = 0,
        _480p = 1,
        _720p = 2,
        _1080p = 3,
        _1440p = 4,
        Portrait360x640 = 5,
        Portrait480x854 = 6,
        Portrait720x1280 = 7,
        Portrait1080x1920 = 8,
        Portrait1440x2560 = 9
    }

    internal static class RecordSettingsQualityInternalEnumExt
    {
        public static RecordSettingsQuality ToPublicEnum(this RecordSettingsQualityInternalEnum internalValue)
        {
            switch (internalValue)
            {
                case RecordSettingsQualityInternalEnum._360p: return RecordSettingsQuality._360p;
                case RecordSettingsQualityInternalEnum._480p: return RecordSettingsQuality._480p;
                case RecordSettingsQualityInternalEnum._720p: return RecordSettingsQuality._720p;
                case RecordSettingsQualityInternalEnum._1080p: return RecordSettingsQuality._1080p;
                case RecordSettingsQualityInternalEnum._1440p: return RecordSettingsQuality._1440p;
                case RecordSettingsQualityInternalEnum.Portrait360x640: return RecordSettingsQuality.Portrait360x640;
                case RecordSettingsQualityInternalEnum.Portrait480x854: return RecordSettingsQuality.Portrait480x854;
                case RecordSettingsQualityInternalEnum.Portrait720x1280: return RecordSettingsQuality.Portrait720x1280;
                case RecordSettingsQualityInternalEnum.Portrait1080x1920: return RecordSettingsQuality.Portrait1080x1920;
                case RecordSettingsQualityInternalEnum.Portrait1440x2560: return RecordSettingsQuality.Portrait1440x2560;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, null);
            }
        }

        public static RecordSettingsQualityInternalEnum ToInternalEnum(this RecordSettingsQuality publicValue)
        {
            switch (publicValue)
            {
                case RecordSettingsQuality._360p: return RecordSettingsQualityInternalEnum._360p;
                case RecordSettingsQuality._480p: return RecordSettingsQualityInternalEnum._480p;
                case RecordSettingsQuality._720p: return RecordSettingsQualityInternalEnum._720p;
                case RecordSettingsQuality._1080p: return RecordSettingsQualityInternalEnum._1080p;
                case RecordSettingsQuality._1440p: return RecordSettingsQualityInternalEnum._1440p;
                case RecordSettingsQuality.Portrait360x640: return RecordSettingsQualityInternalEnum.Portrait360x640;
                case RecordSettingsQuality.Portrait480x854: return RecordSettingsQualityInternalEnum.Portrait480x854;
                case RecordSettingsQuality.Portrait720x1280: return RecordSettingsQualityInternalEnum.Portrait720x1280;
                case RecordSettingsQuality.Portrait1080x1920: return RecordSettingsQualityInternalEnum.Portrait1080x1920;
                case RecordSettingsQuality.Portrait1440x2560: return RecordSettingsQualityInternalEnum.Portrait1440x2560;
                default:
                    throw new ArgumentOutOfRangeException(nameof(publicValue), publicValue, null);
            }
        }
        
        public static RecordSettingsQuality ParseToPublicEnum(string internalValue)
        {
            switch (internalValue)
            {
                case "360p": return RecordSettingsQuality._360p;
                case "480p": return RecordSettingsQuality._480p;
                case "720p": return RecordSettingsQuality._720p;
                case "1080p": return RecordSettingsQuality._1080p;
                case "1440p": return RecordSettingsQuality._1440p;
                case "portrait-360x640": return RecordSettingsQuality.Portrait360x640;
                case "portrait-480x854": return RecordSettingsQuality.Portrait480x854;
                case "portrait-720x1280": return RecordSettingsQuality.Portrait720x1280;
                case "portrait-1080x1920": return RecordSettingsQuality.Portrait1080x1920;
                case "portrait-1440x2560": return RecordSettingsQuality.Portrait1440x2560;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, null);
            }
        }
    }
}