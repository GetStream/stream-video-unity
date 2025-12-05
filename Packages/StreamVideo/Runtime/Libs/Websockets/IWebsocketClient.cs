using System;
using System.Net.WebSockets;
using System.Threading;
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
        
        int ReceiveQueueCount { get; }
        int SendQueueCount { get; }
        bool IsConnected { get; }
        bool IsConnecting { get; }

        bool TryDequeueMessage(out byte[] message);

        Task ConnectAsync(Uri serverUri, CancellationToken cancellationToken);

        void Update();

        void Send(string message);

        void Send(byte[] message);
        
        Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage);

        void ClearSendQueue();
    }
}