using System;
using System.Threading.Tasks;
using StreamVideo.Libs.Auth;
using StreamVideo.Core.Auth;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Handler delegate for a connection state change
    /// </summary>
    public delegate void ConnectionStateChangeHandler(ConnectionState previous, ConnectionState current);
    
    /// <summary>
    /// Stream Chat Client
    /// </summary>
    public interface IStreamVideoLowLevelClient : IAuthProvider, IConnectionProvider, IDisposable
    {
        /// <summary>
        /// Client established WebSockets connection and is ready to send and receive data
        /// </summary>
        event ConnectionHandler Connected;
        
        /// <summary>
        /// Client is attempting to reconnect after lost connection
        /// </summary>
        event Action Reconnecting;

        /// <summary>
        /// Client lost connection with the server. if ReconnectStrategy is Exponential or Constant it will attempt to reconnect.
        /// Once Connected event is raised again you should re-init watch state for previously observed channels and re-fetch potentially missed data
        /// </summary>
        event Action Disconnected;

        /// <summary>
        /// Raised when connection state changes. Returns previous and the current connection state respectively
        /// </summary>
        event ConnectionStateChangeHandler ConnectionStateChanged;

        ConnectionState ConnectionState { get; }
        ReconnectStrategy ReconnectStrategy { get; }
        float ReconnectConstantInterval { get; }
        float ReconnectExponentialMinInterval { get; }
        float ReconnectExponentialMaxInterval { get; }
        double? NextReconnectTime { get; }


        /// <summary>
        /// Per frame update of the StreamChatClient. This method triggers sending and receiving data between the client and the server. Make sure to call it every frame.
        /// </summary>
        /// <param name="deltaTime"></param>
        void Update(float deltaTime);

        /// <summary>
        /// Initiate WebSocket connection with Stream Server.
        /// Use <see cref="IConnectionProvider.Connected"/> to be notified when connection is established
        /// </summary>
        void Connect();

        /// <summary>
        /// Set parameters for StreamChatClient reconnect strategy
        /// </summary>
        /// <param name="reconnectStrategy">Defines how Client will react to Disconnected state</param>
        /// <param name="exponentialMinInterval">Defines min reconnect interval for <see cref="Core.ReconnectStrategy.Exponential"/></param>
        /// <param name="exponentialMaxInterval">Defines max reconnect interval for <see cref="Core.ReconnectStrategy.Exponential"/></param>
        /// <param name="constantInterval">Defines reconnect interval for <see cref="Core.ReconnectStrategy.Constant"/></param>
        /// <exception cref="ArgumentException">throws exception if intervals are less than or equal to zero</exception>
        void SetReconnectStrategySettings(ReconnectStrategy reconnectStrategy, float? exponentialMinInterval,
            float? exponentialMaxInterval, float? constantInterval);

        void ConnectUser(AuthCredentials userAuthCredentials);

        Task DisconnectAsync(bool permanent = false);
    }
}