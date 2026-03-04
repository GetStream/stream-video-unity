using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal interface IPersistentWebSocket<TConnectRequest, TConnectResponse> : IDisposable
    {
        event Action Connected;
        event ConnectionStateChangeHandler ConnectionStateChanged;

        ConnectionState ConnectionState { get; }

        void Update();

        Task<TConnectResponse> ConnectAsync(TConnectRequest request, CancellationToken cancellationToken = default);

        Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage);

        void RegisterEventType<TDto, TEvent>(string key,
            Action<TEvent, TDto> handler, Action<TDto> internalHandler = null)
            where TEvent : ILoadableFrom<TDto, TEvent>, new();

        //StreamTodo: rename to RegisterMessageHandler
        void RegisterEventType<TDto>(string key,
            Action<TDto> internalHandler = null);
    }
}