namespace StreamVideo.Core.Configs
{
    /// <summary>
    /// Configuration for audio streaming
    /// </summary>
    public interface IStreamAudioConfig
    {
        /// <summary>
        /// Enable RED - Increase audio quality in exchange for higher bandwidth because this makes audio packets larger.
        ///
        /// Recommended if users are in areas with lossy network or if audio quality is very important.
        /// Might not be suitable for calls with large number of participants because of the higher bandwidth overhead.
        /// </summary>
        /// <remarks>
        /// https://bloggeek.me/webrtc-media-resilience/#h-red-redundancy-encoding
        /// </remarks>
        public bool EnableRed { get; set; }
        
        /// <summary>
        /// Enable DTX (Discontinuous Transmission) - an Opus Audio Codec extension that will encode silence at lower bitrate.
        /// This can reduce bandwidth and preserve battery on mobile devices.
        ///
        /// Might be a good choice for conferences where participants are not speaking simultaneously.
        /// Not suitable for music streaming. DTX is optimized for human speech and may disrupt music audio quality.
        /// </summary>
        public bool EnableDtx { get; set; }
    }
}