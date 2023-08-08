using System;
using ConnectionQualityInternalEnum = Stream.Video.v1.Sfu.Models.ConnectionQuality;

namespace StreamVideo.Core.Models.Sfu
{
    public enum ConnectionQuality
    {
        Unspecified = 0,
        Poor = 1,
        Good = 2,
        Excellent = 3,
    }
    
    internal static class ConnectionQualityExt
    {
        public static ConnectionQualityInternalEnum ToInternalEnum(this ConnectionQuality connectionQuality)
        {
            switch (connectionQuality)
            {
                case ConnectionQuality.Unspecified: return ConnectionQualityInternalEnum.Unspecified;
                case ConnectionQuality.Poor: return ConnectionQualityInternalEnum.Poor;
                case ConnectionQuality.Good: return ConnectionQualityInternalEnum.Good;
                case ConnectionQuality.Excellent: return ConnectionQualityInternalEnum.Excellent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionQuality), connectionQuality, null);
            }
        }

        public static ConnectionQuality ToPublicEnum(this ConnectionQualityInternalEnum connectionQuality)
        {
            switch (connectionQuality)
            {
                case ConnectionQualityInternalEnum.Unspecified: return ConnectionQuality.Unspecified;
                case ConnectionQualityInternalEnum.Poor: return ConnectionQuality.Poor;
                case ConnectionQualityInternalEnum.Good: return ConnectionQuality.Good;
                case ConnectionQualityInternalEnum.Excellent: return ConnectionQuality.Excellent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionQuality), connectionQuality, null);
            }
        }
    }
}