using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class RecordSettings : IStateLoadableFrom<RecordSettingsInternalDTO, RecordSettings>
    {
        public bool AudioOnly { get; private set;}

        public RecordSettingsMode Mode { get; private set;}

        public RecordSettingsQuality Quality { get; private set;}

        void IStateLoadableFrom<RecordSettingsInternalDTO, RecordSettings>.LoadFromDto(RecordSettingsInternalDTO dto, ICache cache)
        {
            AudioOnly = dto.AudioOnly;
            Mode = dto.Mode.ToPublicEnum();
            Quality = dto.Quality.ToPublicEnum();
        }
    }
}