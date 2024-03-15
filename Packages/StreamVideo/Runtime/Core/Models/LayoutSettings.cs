using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class LayoutSettings : IStateLoadableFrom<LayoutSettingsInternalDTO, LayoutSettings>
    {
        public string ExternalAppUrl { get; set; } = default!;

        public string ExternalCssUrl { get; set; } = default!;

        public LayoutSettingsName Name { get; set; } = default!;

        public IReadOnlyDictionary<string, object> Options => _options;

        void IStateLoadableFrom<LayoutSettingsInternalDTO, LayoutSettings>.LoadFromDto(LayoutSettingsInternalDTO dto, ICache cache)
        {
            ExternalAppUrl = dto.ExternalAppUrl;
            ExternalCssUrl = dto.ExternalCssUrl;
            Name = dto.Name.ToPublicEnum();
            _options.TryReplaceValuesFromDto(dto.Options);
        }
        
        private readonly Dictionary<string, object> _options = new Dictionary<string, object>();
    }
}