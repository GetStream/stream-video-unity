using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class AudioSettings : IStateLoadableFrom<AudioSettingsInternalDTO, AudioSettings>
    {
        public bool AccessRequestEnabled { get; private set;}

        public AudioSettingsDefaultDevice DefaultDevice { get; private set;}

        public bool MicDefaultOn { get; private set;}

        public bool OpusDtxEnabled { get; private set;}

        public bool RedundantCodingEnabled { get; private set;}

        public bool SpeakerDefaultOn { get; private set;}

        void IStateLoadableFrom<AudioSettingsInternalDTO, AudioSettings>.LoadFromDto(AudioSettingsInternalDTO dto, ICache cache)
        {
            AccessRequestEnabled = dto.AccessRequestEnabled;
            DefaultDevice = dto.DefaultDevice.ToPublicEnum();
            MicDefaultOn = dto.MicDefaultOn;
            OpusDtxEnabled = dto.OpusDtxEnabled;
            RedundantCodingEnabled = dto.RedundantCodingEnabled;
            SpeakerDefaultOn = dto.SpeakerDefaultOn;
        }
    }
    
    
}