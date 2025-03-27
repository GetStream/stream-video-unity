using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class AudioSettings : IStateLoadableFrom<AudioSettingsResponseInternalDTO, AudioSettings>
    {
        public bool AccessRequestEnabled { get; private set;}

        public AudioSettingsDefaultDevice DefaultDevice { get; private set;}

        public bool MicDefaultOn { get; private set;}

        public bool OpusDtxEnabled { get; private set;}

        public bool RedundantCodingEnabled { get; private set;}

        public bool SpeakerDefaultOn { get; private set;}

        void IStateLoadableFrom<AudioSettingsResponseInternalDTO, AudioSettings>.LoadFromDto(AudioSettingsResponseInternalDTO dto, ICache cache)
        {
            AccessRequestEnabled = dto.AccessRequestEnabled;
            DefaultDevice = DefaultDevice.TryCreateOrLoadFromDto(dto.DefaultDevice);
            MicDefaultOn = dto.MicDefaultOn;
            OpusDtxEnabled = dto.OpusDtxEnabled;
            RedundantCodingEnabled = dto.RedundantCodingEnabled;
            SpeakerDefaultOn = dto.SpeakerDefaultOn;
        }
    }
    
    
}