namespace StreamVideo.Core.Configs
{
    public class StreamVideoConfig : IStreamVideoConfig
    {
        public VideoResolution DefaultParticipantVideoResolution { get; set; } = VideoResolution.Res_1080p;
    }
}