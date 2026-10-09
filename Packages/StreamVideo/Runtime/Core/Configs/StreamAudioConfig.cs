namespace StreamVideo.Core.Configs
{
    public class StreamAudioConfig : IStreamAudioConfig
    {
        public bool EnableRed { get; set; }
        public bool EnableDtx { get; set; }
        public bool SuspendAudioOnBackground { get; set; }
    }
}