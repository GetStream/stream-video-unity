using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class TranscriptionSettings : IStateLoadableFrom<TranscriptionSettingsInternalDTO, TranscriptionSettings>
    {
        public string ClosedCaptionMode { get; private set;}

        public TranscriptionSettingsMode Mode { get; private set;}

        void IStateLoadableFrom<TranscriptionSettingsInternalDTO, TranscriptionSettings>.LoadFromDto(TranscriptionSettingsInternalDTO dto, ICache cache)
        {
            ClosedCaptionMode = dto.ClosedCaptionMode;
            Mode = dto.Mode.ToPublicEnum();
        }
    }
}