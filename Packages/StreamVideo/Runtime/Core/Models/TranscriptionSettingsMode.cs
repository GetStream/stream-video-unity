using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum TranscriptionSettingsMode
    {
        Available = 0,
        Disabled = 1,
        AutoOn = 2,
    }

    internal static class TranscriptionSettingsModeExt
    {
        public static TranscriptionSettingsModeInternalEnum ToInternalEnum(this
            TranscriptionSettingsMode domainValue)
        {
            switch (domainValue)
            {
                case TranscriptionSettingsMode.Available: return TranscriptionSettingsModeInternalEnum.Available;
                case TranscriptionSettingsMode.Disabled: return TranscriptionSettingsModeInternalEnum.Disabled;
                case TranscriptionSettingsMode.AutoOn: return TranscriptionSettingsModeInternalEnum.AutoOn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainValue), domainValue, null);
            }
        }

        public static TranscriptionSettingsMode ToPublicEnum(this
            TranscriptionSettingsModeInternalEnum internalValue)
        {
            switch (internalValue)
            {
                case TranscriptionSettingsModeInternalEnum.Available: return TranscriptionSettingsMode.Available;
                case TranscriptionSettingsModeInternalEnum.Disabled: return TranscriptionSettingsMode.Disabled;
                case TranscriptionSettingsModeInternalEnum.AutoOn: return TranscriptionSettingsMode.AutoOn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, null);
            }
        }
    }
}