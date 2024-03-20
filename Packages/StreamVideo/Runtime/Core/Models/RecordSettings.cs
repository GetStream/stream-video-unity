using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class RecordSettings : IStateLoadableFrom<RecordSettingsRequestInternalDTO, RecordSettings>,
        IStateLoadableFrom<RecordSettingsResponseInternalDTO, RecordSettings>
    {
        public bool AudioOnly { get; private set; }

        public RecordSettingsMode Mode { get; private set; }

        public RecordSettingsQuality Quality { get; private set; }

        //StreamTodo: check if this is needed, it was probably removed from OpenAPI spec
        void IStateLoadableFrom<RecordSettingsRequestInternalDTO, RecordSettings>.LoadFromDto(RecordSettingsRequestInternalDTO dto,
            ICache cache)
        {
            AudioOnly = dto.AudioOnly;
            Mode = dto.Mode.ToPublicEnum();
            Quality = dto.Quality.ToPublicEnum();
        }
        
        void IStateLoadableFrom<RecordSettingsResponseInternalDTO, RecordSettings>.LoadFromDto(RecordSettingsResponseInternalDTO dto,
            ICache cache)
        {
            AudioOnly = dto.AudioOnly;
            Mode = RecordSettingsModeExt.ParseToPublicEnum(dto.Mode);
            Quality = RecordSettingsQualityInternalEnumExt.ParseToPublicEnum(dto.Quality);
        }
    }
}