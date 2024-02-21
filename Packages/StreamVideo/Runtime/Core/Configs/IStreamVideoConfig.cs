namespace StreamVideo.Core.Configs
{
    /// <summary>
    /// Configuration for audio streaming
    /// </summary>
    public interface IStreamVideoConfig
    {
        VideoResolution DefaultParticipantVideoResolution { get; set; }
    }
}