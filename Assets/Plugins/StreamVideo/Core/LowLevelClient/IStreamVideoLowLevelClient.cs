using System;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Libs.Auth;
using StreamVideo.Core.Auth;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Handler delegate for a connection state change
    /// </summary>
    public delegate void ConnectionStateChangeHandler(ConnectionState previous, ConnectionState current);
    
    //StreamTodo: should probably be internal as well
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

        //StreamTodo: spawn a hidden runner that will call this automatically
        /// <summary>
        /// Per frame update of the StreamChatClient. This method triggers sending and receiving data between the client and the server. Make sure to call it every frame.
        /// </summary>
        void Update();

        Task DisconnectAsync();
        // Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring, bool notify);

        Task ConnectUserAsync(AuthCredentials authCredentials, CancellationToken cancellationToken = default);

        Task ConnectUserAsync(string apiKey, string userId, string userToken,
            CancellationToken cancellationToken = default);

        Task ConnectUserAsync(string apiKey, string userId, ITokenProvider tokenProvider,
            CancellationToken cancellationToken = default);

        Task<string> GetLocationHintAsync();
    }
}