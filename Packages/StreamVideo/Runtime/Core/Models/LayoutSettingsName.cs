using System;
using StreamVideo.Core.InternalDTO.Models;

namespace Core.Models
{
    public enum LayoutSettingsName
    {
        Spotlight = 0,
        Grid = 1,
        SingleParticipant = 2,
        Mobile = 3,
        Custom = 4,
    }
    
    internal static class RecordSettingsModeExt
    {
        public static LayoutSettingsNameInternalEnum ToInternalEnum(this LayoutSettingsName domainValue)
        {
           
            switch (domainValue)
            {
                case LayoutSettingsName.Spotlight: return LayoutSettingsNameInternalEnum.Spotlight;
                case LayoutSettingsName.Grid: return LayoutSettingsNameInternalEnum.Grid;
                case LayoutSettingsName.SingleParticipant: return LayoutSettingsNameInternalEnum.SingleParticipant;
                case LayoutSettingsName.Mobile: return LayoutSettingsNameInternalEnum.Mobile;
                case LayoutSettingsName.Custom: return LayoutSettingsNameInternalEnum.Custom;
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainValue), domainValue, null);
            }
        }

        public static LayoutSettingsName ToPublicEnum(this LayoutSettingsNameInternalEnum internalValue)
        {
            switch (internalValue)
            {
                case LayoutSettingsNameInternalEnum.Spotlight: return LayoutSettingsName.Spotlight;
                case LayoutSettingsNameInternalEnum.Grid: return LayoutSettingsName.Grid;
                case LayoutSettingsNameInternalEnum.SingleParticipant: return LayoutSettingsName.SingleParticipant;
                case LayoutSettingsNameInternalEnum.Mobile: return LayoutSettingsName.Mobile;
                case LayoutSettingsNameInternalEnum.Custom: return LayoutSettingsName.Custom;
                default:
                    throw new ArgumentOutOfRangeException(nameof(internalValue), internalValue, null);
            }
        }
    }
}