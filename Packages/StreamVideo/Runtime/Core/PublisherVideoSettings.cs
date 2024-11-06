namespace StreamVideo.Core
{
    // StreamTodo: Figure out if we should make this public so that developers can control the resolution and FPS. This is troublesome because there needs to be a match between source texture and the video stream
    // So we either take the resolution from the source texture and allow developers to implicitly control this (this could be unintuitive) or we expose this and developers need to ensure that the source texture resolution matches the stream resolution.
    // The second option might be more clear because they'd receive clear errors if there's a video resolution mismatch.
    // Also take into account that we might stream multiple tracks (webcam + screenshare) and we also can have multiple sources (texture, renderTexture, webCamTexture, SceneCamera)
    // Also, perhaps we should allow to publish unlimited number of video tracks and not just webcam + screenshare but any arbitrary combination of video sources.
    
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
        /// Frames per second (FPS) at which the video will be streamed to other participants.
        /// </summary>
        public uint FrameRate { get; set; } = 30;
    }
}