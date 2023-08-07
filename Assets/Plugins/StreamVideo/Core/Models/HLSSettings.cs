using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class HLSSettings : IStateLoadableFrom<HLSSettingsInternalDTO, HLSSettings>
    {
        public bool AutoOn { get; private set;}

        public bool Enabled { get; private set;}

        public IReadOnlyList<string> QualityTracks => _qualityTracks;

        void IStateLoadableFrom<HLSSettingsInternalDTO, HLSSettings>.LoadFromDto(HLSSettingsInternalDTO dto, ICache cache)
        {
            AutoOn = dto.AutoOn;
            Enabled = dto.Enabled;
            _qualityTracks.TryReplaceValuesFromDto(dto.QualityTracks);
        }
        
        private readonly List<string> _qualityTracks = new List<string>();
    }
}