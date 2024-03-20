using System;
using SfuConnectionQuality = StreamVideo.v1.Sfu.Models.ConnectionQuality;

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
        public static SfuConnectionQuality ToInternalEnum(this ConnectionQuality connectionQuality)
        {
            switch (connectionQuality)
            {
                case ConnectionQuality.Unspecified: return SfuConnectionQuality.Unspecified;
                case ConnectionQuality.Poor: return SfuConnectionQuality.Poor;
                case ConnectionQuality.Good: return SfuConnectionQuality.Good;
                case ConnectionQuality.Excellent: return SfuConnectionQuality.Excellent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionQuality), connectionQuality, null);
            }
        }

        public static ConnectionQuality ToPublicEnum(this SfuConnectionQuality connectionQuality)
        {
            switch (connectionQuality)
            {
                case SfuConnectionQuality.Unspecified: return ConnectionQuality.Unspecified;
                case SfuConnectionQuality.Poor: return ConnectionQuality.Poor;
                case SfuConnectionQuality.Good: return ConnectionQuality.Good;
                case SfuConnectionQuality.Excellent: return ConnectionQuality.Excellent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionQuality), connectionQuality, null);
            }
        }
    }
}