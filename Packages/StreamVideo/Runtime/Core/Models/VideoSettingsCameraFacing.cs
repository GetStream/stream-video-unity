using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum VideoSettingsCameraFacing
    {
        Front = 0,
        Back = 1,
        External = 2,
    }
    
    internal static class VideoSettingsCameraFacingExt
    {
        public static VideoSettingsCameraFacingInternalEnum ToInternalEnum(this 
            VideoSettingsCameraFacing videoSettingsCameraFacing)
        {
            switch (videoSettingsCameraFacing)
            {
                case VideoSettingsCameraFacing.Front: return VideoSettingsCameraFacingInternalEnum.Front;
                case VideoSettingsCameraFacing.Back: return VideoSettingsCameraFacingInternalEnum.Back;
                case VideoSettingsCameraFacing.External: return VideoSettingsCameraFacingInternalEnum.External;
                default:
                    throw new ArgumentOutOfRangeException(nameof(videoSettingsCameraFacing), videoSettingsCameraFacing, null);
            }
        }
        
        public static VideoSettingsCameraFacing ToPublicEnum(this 
            VideoSettingsCameraFacingInternalEnum videoSettingsCameraFacing)
        {
            switch (videoSettingsCameraFacing)
            {
                case VideoSettingsCameraFacingInternalEnum.Front: return VideoSettingsCameraFacing.Front;
                case VideoSettingsCameraFacingInternalEnum.Back: return VideoSettingsCameraFacing.Back;
                case VideoSettingsCameraFacingInternalEnum.External: return VideoSettingsCameraFacing.External;
                default:
                    throw new ArgumentOutOfRangeException(nameof(videoSettingsCameraFacing), videoSettingsCameraFacing, null);
            }
        }
    }
}