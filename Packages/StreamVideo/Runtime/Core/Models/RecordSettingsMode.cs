using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum RecordSettingsMode
    {
        Available = 0,
        Disabled = 1,
        AutoOn = 2,
    }

    internal static class RecordSettingsModeExt
    {
        public static RecordSettingsModeInternalEnum ToInternalEnum(this RecordSettingsMode domainValue)
        {
            switch (domainValue)
            {
                case RecordSettingsMode.Available: return RecordSettingsModeInternalEnum.Available;
                case RecordSettingsMode.Disabled: return RecordSettingsModeInternalEnum.Disabled;
                case RecordSettingsMode.AutoOn: return RecordSettingsModeInternalEnum.AutoOn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainValue), domainValue, null);
            }
        }

        public static RecordSettingsMode ToPublicEnum(this RecordSettingsModeInternalEnum internalValue)
        {
            switch (internalValue)
            {
                case RecordSettingsModeInternalEnum.Available: return RecordSettingsMode.Available;
                case RecordSettingsModeInternalEnum.Disabled: return RecordSettingsMode.Disabled;
                case RecordSettingsModeInternalEnum.AutoOn: return RecordSettingsMode.AutoOn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, null);
            }
        }
        
        public static RecordSettingsMode ParseToPublicEnum(string internalValue)
        {
            switch (internalValue)
            {
                case "available": return RecordSettingsMode.Available;
                case "disabled": return RecordSettingsMode.Disabled;
                case "auto-on": return RecordSettingsMode.AutoOn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, $"Failed to parse `{internalValue}` to {typeof(RecordSettingsMode)}");
            }
        }
    }
}