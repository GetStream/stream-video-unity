using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class RecordSettings : IStateLoadableFrom<RecordSettingsResponseInternalDTO, RecordSettings>
    {
        public bool AudioOnly { get; private set; }

        public RecordSettingsMode Mode { get; private set; }

        public RecordSettingsQuality Quality { get; private set; }
        
        void IStateLoadableFrom<RecordSettingsResponseInternalDTO, RecordSettings>.LoadFromDto(RecordSettingsResponseInternalDTO dto,
            ICache cache)
        {
            AudioOnly = dto.AudioOnly;
            Mode = Mode.TryCreateOrLoadFromDto(dto.Mode);
            Quality = Quality.TryCreateOrLoadFromDto(dto.Quality);
        }
    }
}