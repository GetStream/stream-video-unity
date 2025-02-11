using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum TranscriptionSettingsClosedCaptionMode
    {
        Available = 0,
        Disabled = 1,
        AutoOn = 2,
    }
    
    internal static class TranscriptionSettingsClosedCaptionModeExt
    {
        public static TranscriptionSettingsResponseClosedCaptionModeInternalEnum ToInternalEnum(this 
            TranscriptionSettingsClosedCaptionMode videoSettingsCameraFacing)
        {
            switch (videoSettingsCameraFacing)
            {
                case TranscriptionSettingsClosedCaptionMode.Available: return TranscriptionSettingsResponseClosedCaptionModeInternalEnum.Available;
                case TranscriptionSettingsClosedCaptionMode.Disabled: return TranscriptionSettingsResponseClosedCaptionModeInternalEnum.Disabled;
                case TranscriptionSettingsClosedCaptionMode.AutoOn: return TranscriptionSettingsResponseClosedCaptionModeInternalEnum.AutoOn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(videoSettingsCameraFacing), videoSettingsCameraFacing, null);
            }
        }
        
        public static TranscriptionSettingsClosedCaptionMode ToPublicEnum(this 
            TranscriptionSettingsResponseClosedCaptionModeInternalEnum videoSettingsCameraFacing)
        {
            switch (videoSettingsCameraFacing)
            {
                case TranscriptionSettingsResponseClosedCaptionModeInternalEnum.Available: return TranscriptionSettingsClosedCaptionMode.Available;
                case TranscriptionSettingsResponseClosedCaptionModeInternalEnum.Disabled: return TranscriptionSettingsClosedCaptionMode.Disabled;
                case TranscriptionSettingsResponseClosedCaptionModeInternalEnum.AutoOn: return TranscriptionSettingsClosedCaptionMode.AutoOn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(videoSettingsCameraFacing), videoSettingsCameraFacing, null);
            }
        }
    }
       
}