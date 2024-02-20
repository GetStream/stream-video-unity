namespace StreamVideo.Core
{
    //StreamTodo: consider making this public and allowing developers to change the resolution explicitly. Right now it's copied from the passed WebCamTexture
    //But this won't cover cases like screenshare or streaming scene camera
    /// <summary>
    /// Settings related to video stream that's sent to other participants.
    /// </summary>
    internal class PublisherVideoSettings
    {
        public static PublisherVideoSettings Default { get; } = new PublisherVideoSettings();
        
        /// <summary>
        /// Max resolution at which the video will be streamed to other participants.
        /// The final resolution depends on factors like network bandwidth and traffic.
        /// Stream will automatically adjust the resolution to network conditions in order to ensure smooth video as much as possible.
        /// </summary>
        public VideoResolution MaxResolution { get; set; } = VideoResolution.Res_720p;
        
        /// <summary>
        /// Max frames per second at which the video will be streamed to other participants.
        /// The final frame rate depends on factors like network bandwidth and traffic.
        /// Stream will automatically adjust the resolution to network conditions in order to ensure smooth video as much as possible.
        /// </summary>
        public uint FrameRate { get; set; } = 30;

        internal void CopyValuesFrom(PublisherVideoSettings source)
        {
            MaxResolution = source.MaxResolution;
            FrameRate = source.FrameRate;
        }
    }
}