using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class GeofenceSettings : IStateLoadableFrom<GeofenceSettingsInternalDTO, GeofenceSettings>
    {
        public IReadOnlyList<string> Names => _names;

        void IStateLoadableFrom<GeofenceSettingsInternalDTO, GeofenceSettings>.LoadFromDto(GeofenceSettingsInternalDTO dto, ICache cache)
        {
            _names.TryReplaceValuesFromDto(dto.Names);
        }

        private readonly List<string> _names = new List<string>();
    }
}