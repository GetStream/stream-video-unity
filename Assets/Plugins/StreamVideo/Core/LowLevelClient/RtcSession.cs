using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Stream.Video.v1.Sfu.Signal;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Utils;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    //StreamTodo: reconnect flow needs to send `UpdateSubscription` https://getstream.slack.com/archives/C022N8JNQGZ/p1691139853890859?thread_ts=1691139571.281779&cid=C022N8JNQGZ
    
    //StreamTodo: decide lifetime, if the obj persists across session maybe it should be named differently and only return struct handle to a session
    internal sealed class RtcSession : IDisposable
    {
        public RtcSession(SfuWebSocket sfuWebSocket, ILogs logs)
        {
            _logs = logs;
            
            //StreamTodo: SFU WS should be created here so that RTC session owns it
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

            _peerConnection = new RTCPeerConnection(ref conf);
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
            
            _peerConnection.Close();

            _sendChannel.OnOpen -= OnSendChannelStatusChanged;
            _sendChannel.OnClose -= OnSendChannelStatusChanged;
            _sendChannel.OnMessage -= OnSendChannelMessage;
            
            _sendChannel.Close();
        }

        public void Update()
        {
            _sfuWebSocket.Update();
            
            WebRTC.Update().MoveNext();
        }

        public async Task StartAsync(JoinCallResponseInternalDTO joinCallResponse)
        {
            //StreamTodo: check if not started already
            
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
            _peerConnection.SetLocalDescription(ref offer);
            
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
            _logs.Warning("$$$$$$$ OnIceCandidate");
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            _logs.Warning("$$$$$$$ OnIceConnectionChange");
        }

        private void OnNegotiationNeeded()
        {
            _logs.Warning("$$$$$$$ OnNegotiationNeeded");
            
            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            _logs.Warning("$$$$$$$ OnConnectionStateChange");
        }

        private void OnTrack(RTCTrackEvent e)
        {
            _logs.Warning("$$$$$$$ OnTrack");
        }

        private void OnDataChannel(RTCDataChannel channel)
        {
            _logs.Warning("$$$$$$$ OnDataChannel");
        }

        private void OnSendChannelStatusChanged()
        {
            _logs.Warning("$$$$$$$ OnSendChannelStatusChanged");
        }

        private void OnSendChannelMessage(byte[] bytes)
        {
            _logs.Warning("$$$$$$$ OnSendChannelMessage");
        }

        private List<TrackSubscriptionDetails> _subscribedTracks = new List<TrackSubscriptionDetails>();
        
        /** From Android setVideoSubscriptions()
     * Tells the SFU which video tracks we want to subscribe to
     * - it sends the resolutions we're displaying the video at so the SFU can decide which track to send
     * - when switching SFU we should repeat this info
     * - http calls failing here breaks the call. (since you won't receive the video)
     * - we should retry continously until it works and after it continues to fail, raise an error that shuts down the call
     * - we retry when:
     * -- error isn't permanent, SFU didn't change, the mute/publish state didn't change
     * -- we cap at 30 retries to prevent endless loops
     */
        private void getVideoSubscriptions()
        {
            var request = new UpdateSubscriptionsRequest
            {
                SessionId = _sessionId,
                Tracks = { _subscribedTracks }
            };
        }

    }
}