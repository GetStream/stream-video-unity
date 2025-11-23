using System;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core
{
    /// <summary>
    /// <see cref="IStreamVideoLowLevelClient"/> connection state
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// StreamChatClient is Disconnected from the server.
        /// If was Connected before and allowed by <see cref="IStreamVideoLowLevelClient.ReconnectStrategy"/> it will switch to WaitToReconnect and attempt to connect again after a timeout
        /// </summary>
        Disconnected,
        
        /// <summary>
        /// Disconnecting current user in progress. Client can still reconnect after this.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// Currently connecting to server, waiting for the connection handshake to complete
        /// </summary>
        Connecting,

        /// <summary>
        /// Waiting specified interval until the next connect attempt
        /// </summary>
        WaitToReconnect,

        /// <summary>
        /// Connection with server is active. StreamChatClient can send and receive data
        /// </summary>
        Connected,

        /// <summary>
        /// Connection is permanently closing. There will be no reconnects made. StreamVideoClient is probably being disposed
        /// </summary>
        Closing,
    }

    /// <summary>
    /// Extensions for <see cref="ConnectionState"/>
    /// </summary>
    internal static class ConnectionStateExt
    {
        public static bool IsValidToConnect(this ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                case ConnectionState.Closing:
                case ConnectionState.Disconnecting:
                    return false;
                case ConnectionState.Disconnected:
                case ConnectionState.WaitToReconnect:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}