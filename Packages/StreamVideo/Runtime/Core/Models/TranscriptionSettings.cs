using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class TranscriptionSettings : IStateLoadableFrom<TranscriptionSettingsResponseInternalDTO, TranscriptionSettings>
    {
        public string ClosedCaptionMode { get; private set;}

        public TranscriptionSettingsMode Mode { get; private set;}

        void IStateLoadableFrom<TranscriptionSettingsResponseInternalDTO, TranscriptionSettings>.LoadFromDto(TranscriptionSettingsResponseInternalDTO dto, ICache cache)
        {
            ClosedCaptionMode = dto.ClosedCaptionMode;
            Mode = dto.Mode.ToPublicEnum();
        }
    }
}