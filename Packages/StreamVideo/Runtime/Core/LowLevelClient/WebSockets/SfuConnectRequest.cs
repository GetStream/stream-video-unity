using StreamVideo.v1.Sfu.Events;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    /// <summary>
    /// Encapsulates the data needed to establish or re-establish a connection to the SFU,
    /// including reconnect details such as strategy, attempt count, and previous session information.
    /// </summary>
    internal struct SfuConnectRequest
    {
        public ReconnectDetails ReconnectDetails;
    }
}
