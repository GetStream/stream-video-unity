namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    /// <summary>
    /// Factory for creating <see cref="ISfuWebSocket"/> instances.
    /// This enables <see cref="RtcSession"/> to create new SFU WebSocket connections
    /// when needed (e.g., for reconnection or migration scenarios).
    /// </summary>
    internal interface ISfuWebSocketFactory
    {
        /// <summary>
        /// Creates a new <see cref="ISfuWebSocket"/> instance.
        /// </summary>
        ISfuWebSocket Create();
    }
}

