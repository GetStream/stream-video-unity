using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Configs
{
    /// <summary>
    /// Configuration for <see cref="IStreamVideoLowLevelClient"/>
    /// </summary>
    public interface IStreamClientConfig
    {
        /// <summary>
        /// What type of logs are being emitted. Available options:
        /// FailureOnly - only errors will be logged. This option is recommended for production
        /// All - all errors will be logged. This can be useful during development
        /// Debug - This included All logs + some additional that can be useful for debugging
        /// Disabled - no logs will be emitted. Not recommended in general - this could be only viable if you're capturing all of the thrown exceptions and handling the logging on your own.
        /// </summary>
        StreamLogLevel LogLevel { get; set; }

        IStreamAudioConfig Audio { get; }
    }
}