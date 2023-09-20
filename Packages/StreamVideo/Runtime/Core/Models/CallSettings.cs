using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallSettings : IStateLoadableFrom<CallSettingsResponseInternalDTO, CallSettings>
    {
        public AudioSettings Audio { get; private set;}

        public BackstageSettings Backstage { get; private set;}

        public BroadcastSettings Broadcasting { get; private set;}

        public GeofenceSettings Geofencing { get; private set;}

        public RecordSettings Recording { get; private set;}

        public RingSettings Ring { get; private set;}

        public ScreensharingSettings Screensharing { get; private set;}

        public TranscriptionSettings Transcription { get; private set;}

        public VideoSettings Video { get; private set;}

        void IStateLoadableFrom<CallSettingsResponseInternalDTO, CallSettings>.LoadFromDto(CallSettingsResponseInternalDTO dto, ICache cache)
        {
            Audio = cache.TryUpdateOrCreateFromDto(Audio,dto.Audio);
            Backstage = cache.TryUpdateOrCreateFromDto(Backstage,dto.Backstage);
            Broadcasting = cache.TryUpdateOrCreateFromDto(Broadcasting,dto.Broadcasting);
            Geofencing = cache.TryUpdateOrCreateFromDto(Geofencing,dto.Geofencing);
            Recording = cache.TryUpdateOrCreateFromDto(Recording,dto.Recording);
            Ring = cache.TryUpdateOrCreateFromDto(Ring,dto.Ring);
            Screensharing = cache.TryUpdateOrCreateFromDto(Screensharing,dto.Screensharing);
            Transcription = cache.TryUpdateOrCreateFromDto(Transcription,dto.Transcription);
            Video = cache.TryUpdateOrCreateFromDto(Video,dto.Video);
        }
    }
}