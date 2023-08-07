using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum AudioSettingsDefaultDevice
    {
        Speaker = 0,
        Earpiece = 1,
    }

    internal static class AudioSettingsDefaultDeviceExt
    {
        public static AudioSettingsDefaultDeviceInternalEnum ToInternalEnum(
            this AudioSettingsDefaultDevice audioSettingsDefaultDevice)
        {
            switch (audioSettingsDefaultDevice)
            {
                case AudioSettingsDefaultDevice.Speaker: return AudioSettingsDefaultDeviceInternalEnum.Speaker;
                case AudioSettingsDefaultDevice.Earpiece: return AudioSettingsDefaultDeviceInternalEnum.Earpiece;
                default:
                    throw new ArgumentOutOfRangeException(nameof(audioSettingsDefaultDevice),
                        audioSettingsDefaultDevice, null);
            }
        }

        public static AudioSettingsDefaultDevice ToPublicEnum(
            this AudioSettingsDefaultDeviceInternalEnum audioSettingsDefaultDevice)
        {
            switch (audioSettingsDefaultDevice)
            {
                case AudioSettingsDefaultDeviceInternalEnum.Speaker: return AudioSettingsDefaultDevice.Speaker;
                case AudioSettingsDefaultDeviceInternalEnum.Earpiece: return AudioSettingsDefaultDevice.Earpiece;
                default:
                    throw new ArgumentOutOfRangeException(nameof(audioSettingsDefaultDevice),
                        audioSettingsDefaultDevice, null);
            }
        }
    }
}