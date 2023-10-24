using System.Collections.Generic;
using Core.Models;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class HLSSettings : IStateLoadableFrom<HLSSettingsInternalDTO, HLSSettings>,
        IStateLoadableFrom<HLSSettingsResponseInternalDTO, HLSSettings>
    {
        public bool AutoOn { get; private set; }

        public bool Enabled { get; private set; }
        
        public LayoutSettings Layout { get; private set; }

        public IReadOnlyList<string> QualityTracks => _qualityTracks;

        void IStateLoadableFrom<HLSSettingsInternalDTO, HLSSettings>.LoadFromDto(HLSSettingsInternalDTO dto,
            ICache cache)
        {
            AutoOn = dto.AutoOn;
            Enabled = dto.Enabled;
            _qualityTracks.TryReplaceValuesFromDto(dto.QualityTracks);
        }

        void IStateLoadableFrom<HLSSettingsResponseInternalDTO, HLSSettings>.LoadFromDto(HLSSettingsResponseInternalDTO dto, ICache cache)
        {
            AutoOn = dto.AutoOn;
            Enabled = dto.Enabled;
            Layout = cache.TryUpdateOrCreateFromDto(Layout, dto.Layout);
            _qualityTracks.TryReplaceValuesFromDto(dto.QualityTracks);
        }

        private readonly List<string> _qualityTracks = new List<string>();
    }
}