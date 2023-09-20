using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal interface IPersistentWebSocket : IDisposable
    {
        event ConnectionStateChangeHandler ConnectionStateChanged;

        ConnectionState ConnectionState { get; }

        void Update();

        Task ConnectAsync(CancellationToken cancellationToken = default);

        Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage);

        void RegisterEventType<TDto, TEvent>(string key,
            Action<TEvent, TDto> handler, Action<TDto> internalHandler = null)
            where TEvent : ILoadableFrom<TDto, TEvent>, new();

        void RegisterEventType<TDto>(string key,
            Action<TDto> internalHandler = null);
    }
}