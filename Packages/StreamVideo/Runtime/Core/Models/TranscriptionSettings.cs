using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class TranscriptionSettings : IStateLoadableFrom<TranscriptionSettingsResponseInternalDTO, TranscriptionSettings>
    {
        public TranscriptionSettingsClosedCaptionMode ClosedCaptionMode { get; private set;}

        public TranscriptionSettingsMode Mode { get; private set;}

        void IStateLoadableFrom<TranscriptionSettingsResponseInternalDTO, TranscriptionSettings>.LoadFromDto(TranscriptionSettingsResponseInternalDTO dto, ICache cache)
        {
            ClosedCaptionMode = dto.ClosedCaptionMode.ToPublicEnum();
            Mode = dto.Mode.ToPublicEnum();
        }
    }
}