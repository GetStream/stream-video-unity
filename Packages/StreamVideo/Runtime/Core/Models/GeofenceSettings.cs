using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class GeofenceSettings : IStateLoadableFrom<GeofenceSettingsResponseInternalDTO, GeofenceSettings>
    {
        public IReadOnlyList<string> Names => _names;

        void IStateLoadableFrom<GeofenceSettingsResponseInternalDTO, GeofenceSettings>.LoadFromDto(GeofenceSettingsResponseInternalDTO dto, ICache cache)
        {
            _names.TryReplaceValuesFromDto(dto.Names);
        }

        private readonly List<string> _names = new List<string>();
    }
}