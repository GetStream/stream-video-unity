using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class TranscriptionSettings : IStateLoadableFrom<TranscriptionSettingsResponseInternalDTO, TranscriptionSettings>
    {
        public ClosedCaptionMode ClosedCaptionMode { get; private set;}

        public TranscriptionSettingsMode Mode { get; private set;}

        void IStateLoadableFrom<TranscriptionSettingsResponseInternalDTO, TranscriptionSettings>.LoadFromDto(TranscriptionSettingsResponseInternalDTO dto, ICache cache)
        {
            ClosedCaptionMode = ClosedCaptionMode.TryCreateOrLoadFromDto(dto.ClosedCaptionMode);
            Mode = Mode.TryCreateOrLoadFromDto(dto.Mode);
        }
    }
}