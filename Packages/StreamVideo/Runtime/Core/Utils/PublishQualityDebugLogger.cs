using StreamVideo.Libs.Logs;
using Unity.WebRTC;
using SfuEvents = StreamVideo.v1.Sfu.Events;

namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// Compact debug logging for ChangePublishQuality events.
    /// Delegates to <see cref="SimulcastDebugLogger"/> when STREAM_DEBUG_ENABLED.
    /// </summary>
    internal static class PublishQualityDebugLogger
    {
        public static void LogSfuRequest(ILogs logs, string peerType,
            SfuEvents.VideoSender videoSenderSettings)
        {
#if STREAM_DEBUG_ENABLED
            SimulcastDebugLogger.LogChangePublishQualityRequest(logs, $"peer={peerType}",
                videoSenderSettings);
#endif
        }

        public static void LogStateBefore(ILogs logs, string peerType,
            RTCRtpSendParameters parameters)
        {
#if STREAM_DEBUG_ENABLED
            SimulcastDebugLogger.LogPublisherEncodingState(logs, $"peer={peerType}", "before", parameters);
#endif
        }

        public static void LogStateAfter(ILogs logs, string peerType,
            RTCRtpSendParameters parameters)
        {
#if STREAM_DEBUG_ENABLED
            SimulcastDebugLogger.LogPublisherEncodingState(logs, $"peer={peerType}", "after", parameters);
#endif
        }
    }
}
