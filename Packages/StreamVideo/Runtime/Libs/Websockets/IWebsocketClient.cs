using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace StreamVideo.Libs.Websockets
{
    /// <summary>
    /// Client that communicates with server using websockets protocol
    /// </summary>
    public interface IWebsocketClient : IDisposable
    {
        event Action Connected;
        event Action Disconnected;
        event Action ConnectionFailed;
        int QueuedMessagesCount { get; }

        bool TryDequeueMessage(out byte[] message);

        Task ConnectAsync(Uri serverUri);

        void Update();

        void Send(string message);

        void Send(byte[] message);
        
        Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage);
    }
}