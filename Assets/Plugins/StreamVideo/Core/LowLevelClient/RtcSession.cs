using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Utils;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    //StreamTodo: decide lifetime, if the obj persists across session maybe it should be named differently and only return struct handle to a session
    internal class RtcSession : IDisposable
    {
        public RtcSession(SfuWebSocket sfuWebSocket, ILogs logs)
        {
            _logs = logs;
            
            //SFU WS should be created here so that RTC session owns it
            _sfuWebSocket = sfuWebSocket ?? throw new ArgumentNullException(nameof(sfuWebSocket));
            
            var conf = new RTCConfiguration
            {
                iceServers = new RTCIceServer[]
                {
                    new RTCIceServer
                    {
                        credential = null,
                        credentialType = RTCIceCredentialType.Password,
                        urls = new string[]
                        {
                            // Google Stun server
                            "stun:stun.l.google.com:19302"
                        },
                        username = null
                    }
                },
                iceTransportPolicy = null,
                bundlePolicy = null,
                iceCandidatePoolSize = null
            };

            _peerConnection = new RTCPeerConnection();
            _peerConnection.OnIceCandidate += OnIceCandidate;
            _peerConnection.OnIceConnectionChange += OnIceConnectionChange;
            _peerConnection.OnNegotiationNeeded += OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange += OnConnectionStateChange;
            _peerConnection.OnTrack += OnTrack;
            _peerConnection.OnDataChannel += OnDataChannel;
            
            _sendChannel = _peerConnection.CreateDataChannel("sendChannel");

            _sendChannel.OnOpen += OnSendChannelStatusChanged;
            _sendChannel.OnClose += OnSendChannelStatusChanged;
            _sendChannel.OnMessage += OnSendChannelMessage;
            
            _videoTransceiver = _peerConnection.AddTransceiver(TrackKind.Video);
            _audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio);
        }

        public void Dispose()
        {
            StopAsync().LogIfFailed();
            _sfuWebSocket.Dispose();
            
            _peerConnection.OnIceCandidate -= OnIceCandidate;
            _peerConnection.OnIceConnectionChange -= OnIceConnectionChange;
            _peerConnection.OnNegotiationNeeded -= OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange -= OnConnectionStateChange;
            _peerConnection.OnTrack -= OnTrack;
            _peerConnection.OnDataChannel -= OnDataChannel;
            
            _sendChannel.OnOpen -= OnSendChannelStatusChanged;
            _sendChannel.OnClose -= OnSendChannelStatusChanged;
            _sendChannel.OnMessage -= OnSendChannelMessage; 
        }

        public void Update()
        {
            _sfuWebSocket.Update();
        }

        public async Task StartAsync(JoinCallResponse joinCallResponse)
        {
            var sfuUrl = joinCallResponse.Credentials.Server.Url;
            var sfuToken = joinCallResponse.Credentials.Token;
            var iceServers = joinCallResponse.Credentials.IceServers;
            //StreamTodo: what to do with iceServers?

#if STREAM_DEBUG_ENABLED
            _logs.Warning(sfuUrl);
            _logs.Warning(sfuToken);
#endif
            
            
            _sessionId = Guid.NewGuid().ToString();
            
            var offer = await _peerConnection.CreateOfferAsync();
            
            _sfuWebSocket.SetSessionData(_sessionId, offer.sdp, sfuUrl, sfuToken);
            await _sfuWebSocket.ConnectAsync();
        }

        public async Task StopAsync()
        {
            await _sfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure, "Video session stopped");
        }
        
        private readonly RTCPeerConnection _peerConnection;
        private readonly RTCDataChannel _sendChannel;
        private readonly RTCRtpTransceiver _videoTransceiver;
        private readonly RTCRtpTransceiver _audioTransceiver;
        private readonly SfuWebSocket _sfuWebSocket;
        
        private readonly ILogs _logs;
        private string _sessionId;

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
        }

        private void OnNegotiationNeeded()
        {
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
        }

        private void OnTrack(RTCTrackEvent e)
        {
        }

        private void OnDataChannel(RTCDataChannel channel)
        {
        }

        private void OnSendChannelStatusChanged()
        {
        }

        private void OnSendChannelMessage(byte[] bytes)
        {
        }


    }
}