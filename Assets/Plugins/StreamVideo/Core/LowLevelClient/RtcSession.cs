using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Stream.Video.v1.Sfu.Events;
using Stream.Video.v1.Sfu.Models;
using Stream.Video.v1.Sfu.Signal;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.Utils;
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
            _sfuWebSocket.JoinResponse += OnSfuJoinResponse;

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
            _sfuWebSocket.JoinResponse -= OnSfuJoinResponse;

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

        public async Task StartAsync(IStreamCall call)
        {
            if (_activeCall != null)
            {
                throw new InvalidOperationException($"Cannot start new session until previous call is active. Active call: {_activeCall}");
            }

            _activeCall = call ?? throw new ArgumentNullException(nameof(call));
            //StreamTodo: check if not started already

            _callingState = CallingState.Joining;

            var sfuUrl = call.Credentials.Server.Url;
            var sfuToken = call.Credentials.Token;
            var iceServers = call.Credentials.IceServers;
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

            while (_callingState != CallingState.Joined)
            {
                //StreamTodo: implement a timeout if something goes wrong
                //StreamTodo: implement cancellation token
                await Task.Delay(1);
            }
            
            await SubscribeToTracksAsync();

            //StreamTodo: validate when this state should set
            _callingState = CallingState.Joined;
        }

        public async Task StopAsync()
        {
            //StreamTodo: check with js definition of "offline" 
            _callingState = CallingState.Offline;
            await _sfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure, "Video session stopped");
        }

        private readonly RTCPeerConnection _peerConnection;
        private readonly RTCDataChannel _sendChannel;
        private readonly RTCRtpTransceiver _videoTransceiver;
        private readonly RTCRtpTransceiver _audioTransceiver;
        private readonly SfuWebSocket _sfuWebSocket;
        private readonly ILogs _logs;
        
        private string _sessionId;
        private IStreamCall _activeCall;
        private CallingState _callingState;
        
        private async Task SubscribeToTracksAsync()
        {
            _logs.Info("Request SFU - UpdateSubscriptionsRequest");
            var tracks = GetDesiredTracksDetails();

            var request = new UpdateSubscriptionsRequest
            {
                SessionId = _sessionId,
            };
            
            request.Tracks.AddRange(tracks);
            
            _sfuWebSocket.Send(request);
        }

        private IEnumerable<TrackSubscriptionDetails> GetDesiredTracksDetails()
        {
            //StreamTodo: inject info on what tracks we want and what dimensions
            var trackTypes = new[] { TrackType.Video, TrackType.Audio };
            
            foreach (var participant in _activeCall.Participants)
            {
                foreach (var trackType in trackTypes)
                {
                    yield return new TrackSubscriptionDetails
                    {
                        UserId = participant.UserId,
                        SessionId = participant.SessionId,

                        TrackType = trackType,
                        Dimension = new VideoDimension
                        {
                            Width = 600,
                            Height = 600
                        }
                    };
                }
            }
        }

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

        private void OnSfuJoinResponse(JoinResponse joinResponse)
        {
            _logs.InfoIfDebug($"Handle Sfu {nameof(JoinResponse)}");
            ((StreamCall)_activeCall).UpdateFromSfu(joinResponse);
            _callingState = CallingState.Joined;
        }
    }
}