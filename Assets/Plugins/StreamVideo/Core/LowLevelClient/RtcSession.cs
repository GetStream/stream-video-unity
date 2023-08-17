using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Stream.Video.v1.Sfu.Events;
using Stream.Video.v1.Sfu.Models;
using Stream.Video.v1.Sfu.Signal;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.Models;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Utils;
using Unity.WebRTC;
using ICETrickle = Stream.Video.v1.Sfu.Models.ICETrickle;
using TrackType = Stream.Video.v1.Sfu.Models.TrackType;

namespace StreamVideo.Core.LowLevelClient
{
    public delegate void ParticipantTrackChangedHandler(IStreamVideoCallParticipant participant, IStreamTrack track);
    
    //StreamTodo: reconnect flow needs to send `UpdateSubscription` https://getstream.slack.com/archives/C022N8JNQGZ/p1691139853890859?thread_ts=1691139571.281779&cid=C022N8JNQGZ

    //StreamTodo: decide lifetime, if the obj persists across session maybe it should be named differently and only return struct handle to a session
    internal sealed class RtcSession : IDisposable
    {
        public event ParticipantTrackChangedHandler TrackAdded;

        public CallingState CallState
        {
            get => _callState;
            private set
            {
                if (_callState == value)
                {
                    return;
                }

                var prevState = _callState;
                _callState = value;
                _logs.Warning($"Call state changed from {prevState} to {value}");
            }
        }

        public RtcSession(SfuWebSocket sfuWebSocket, ILogs logs, ISerializer serializer, IHttpClient httpClient)
        {
            _serializer = serializer;
            _httpClient = httpClient;
            _logs = logs;

            //StreamTodo: SFU WS should be created here so that RTC session owns it
            _sfuWebSocket = sfuWebSocket ?? throw new ArgumentNullException(nameof(sfuWebSocket));
            _sfuWebSocket.JoinResponse += OnSfuJoinResponse;
            _sfuWebSocket.IceTrickle += OnSfuIceTrickle;
            _sfuWebSocket.SubscriberOffer += OnSfuSubscriberOffer;
        }

        public void Dispose()
        {
            StopAsync().LogIfFailed();

            _sfuWebSocket.JoinResponse -= OnSfuJoinResponse;
            _sfuWebSocket.IceTrickle -= OnSfuIceTrickle;
            _sfuWebSocket.SubscriberOffer -= OnSfuSubscriberOffer;
            _sfuWebSocket.Dispose();

            DisposeSubscriber();
            DisposePublisher();
        }

        public void Update()
        {
            _sfuWebSocket.Update();
        }

        public async Task StartAsync(IStreamCall call)
        {
            if (_activeCall != null)
            {
                throw new InvalidOperationException(
                    $"Cannot start new session until previous call is active. Active call: {_activeCall}");
            }

            _activeCall = call ?? throw new ArgumentNullException(nameof(call));

            CleanUpSession();

            CallState = CallingState.Joining;

            var sfuUrl = call.Credentials.Server.Url;
            var sfuToken = call.Credentials.Token;
            var iceServers = call.Credentials.IceServers;
            //StreamTodo: what to do with iceServers?
            
            CreateSubscriber(iceServers);

            if (CanPublish())
            {
                CreatePublisher(iceServers);
            }

            _sessionId = Guid.NewGuid().ToString();
            _logs.Warning($"START Session: " + _sessionId);

            var offer = await _subscriber.CreateOfferAsync();
            
            //Kotlin is not doing this
            //await _subscriber.SetLocalDescriptionAsync(ref offer);

            _sfuWebSocket.SetSessionData(_sessionId, offer.sdp, sfuUrl, sfuToken);
            await _sfuWebSocket.ConnectAsync();

            while (CallState != CallingState.Joined)
            {
                //StreamTodo: implement a timeout if something goes wrong
                //StreamTodo: implement cancellation token
                await Task.Delay(1);
            }

            await SubscribeToTracksAsync();

            //StreamTodo: validate when this state should set
            CallState = CallingState.Joined;
        }

        public async Task StopAsync()
        {
            CleanUpSession();
            //StreamTodo: check with js definition of "offline" 
            CallState = CallingState.Offline;
            await _sfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure, "Video session stopped");
        }

        //StreamTodo: call by call.reconnectOrSwitchSfu()
        public void Reconnect()
        {
            _subscriber?.RestartIce();
            _publisher?.RestartIce();
        }

        private readonly SfuWebSocket _sfuWebSocket;
        private readonly ISerializer _serializer;
        private readonly ILogs _logs;

        private readonly List<ICETrickle> _pendingIceTrickleRequests = new List<ICETrickle>();

        private string _sessionId;
        private IStreamCall _activeCall;
        private IHttpClient _httpClient;
        private CallingState _callState;

        private StreamPeerConnection _subscriber;
        private StreamPeerConnection _publisher;

        private void CleanUpSession()
        {
            _pendingIceTrickleRequests.Clear();
        }

        private async Task SubscribeToTracksAsync()
        {
            _logs.Info("Request SFU - UpdateSubscriptionsRequest");
            var tracks = GetDesiredTracksDetails();

            var request = new UpdateSubscriptionsRequest
            {
                SessionId = _sessionId,
            };

            request.Tracks.AddRange(tracks);

            var response = await RpcCallAsync(request, GeneratedAPI.UpdateSubscriptions,
                nameof(GeneratedAPI.UpdateSubscriptions));

            if (response.Error != null)
            {
                _logs.Error(response.Error.Message);
            }
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

        private async Task SendIceCandidateAsync(RTCIceCandidate candidate, StreamPeerType streamPeerType)
        {
            try
            {
                var iceTrickle = new ICETrickle
                {
                    PeerType = streamPeerType.ToPeerType(),
                    IceCandidate = _serializer.Serialize(candidate),
                    SessionId = _sessionId,
                };

                if (_callState == CallingState.Joined)
                {
                    await RpcCallAsync(iceTrickle, GeneratedAPI.IceTrickle, nameof(GeneratedAPI.IceTrickle));
                }
                else
                {
                    _pendingIceTrickleRequests.Add(iceTrickle);
                }
            }
            catch (Exception e)
            {
                _logs.Exception(e);
            }
        }

        private void OnSfuJoinResponse(JoinResponse joinResponse)
        {
            _logs.InfoIfDebug($"Handle Sfu {nameof(JoinResponse)}");
            ((StreamCall)_activeCall).UpdateFromSfu(joinResponse);
            OnSfuJoinedCall();
        }

        private void OnSfuJoinedCall()
        {
            CallState = CallingState.Joined;

            foreach (var iceTrickle in _pendingIceTrickleRequests)
            {
                RpcCallAsync(iceTrickle, GeneratedAPI.IceTrickle, nameof(GeneratedAPI.IceTrickle)).LogIfFailed();
            }
        }

        private void OnSfuIceTrickle(ICETrickle iceTrickle)
        {
            //StreamTodo: better to wrap in separate structure and not depend on a specific WebRTC implementation
            var iceCandidateInit = _serializer.Deserialize<RTCIceCandidateInit>(iceTrickle.IceCandidate);

            switch (iceTrickle.PeerType.ToStreamPeerType())
            {
                case StreamPeerType.Publisher:
                    _publisher.AddIceCandidate(iceCandidateInit);
                    break;
                case StreamPeerType.Subscriber:
                    _subscriber.AddIceCandidate(iceCandidateInit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /**
     * This is called when the SFU sends us an offer
     * - Sets the remote description
     * - Creates an answer
     * - Sets the local description
     * - Sends the answer back to the SFU
     */
        private async void OnSfuSubscriberOffer(SubscriberOffer subscriberOffer)
        {
            //StreamTodo: check RtcSession.kt handleSubscriberOffer for the retry logic
            
            try
            {
                
                //StreamTodo: handle subscriberOffer.iceRestart
                var rtcSessionDescription = new RTCSessionDescription
                {
                    type = RTCSdpType.Offer,
                    sdp = subscriberOffer.Sdp
                };

                await _subscriber.SetRemoteDescriptionAsync(rtcSessionDescription);

                var answer = await _subscriber.CreateAnswerAsync();
                
                //StreamTodo: mangle SDP

                await _subscriber.SetLocalDescriptionAsync(ref answer);

                var sendAnswerRequest = new SendAnswerRequest
                {
                    PeerType = PeerType.Subscriber,
                    Sdp = answer.sdp,
                    SessionId = _sessionId
                };

                await RpcCallAsync(sendAnswerRequest, GeneratedAPI.SendAnswer, nameof(GeneratedAPI.SendAnswer), preLog: true);

            }
            catch (Exception e)
            {
                _logs.Exception(e);
            }
        }

        private async Task<TResponse> RpcCallAsync<TRequest, TResponse>(TRequest request,
            Func<HttpClient, TRequest, Task<TResponse>> rpcCallAsync, string debugRequestName, bool preLog = false)
        {
            var serializedRequest = _serializer.Serialize(request);

            if (preLog)
            {
                _logs.Warning($"[RPC REQUEST START] {debugRequestName} {serializedRequest}");
            }

            //StreamTodo: use injected client or cache this one
            var connectUrl = _activeCall.Credentials.Server.Url.Replace("/twirp", "");

            //StreamTodo: move headers population logic elsewhere + remove duplication with main client
            var httpClient = new HttpClient()
            {
                DefaultRequestHeaders =
                {
                    { "stream-auth-type", "jwt" },
                    { "X-Stream-Client", "stream-video-unity-client-0.1.0" }
                }
            };

            httpClient.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue(_activeCall.Credentials.Token);
            httpClient.BaseAddress = new Uri(connectUrl);

            var response = await rpcCallAsync(httpClient, request);
            var serializedResponse = _serializer.Serialize(response);

#if STREAM_DEBUG_ENABLED
            //StreamTodo: move to debug helper class
            var sb = new StringBuilder();

            var errorProperty = typeof(TResponse).GetProperty("Error");
            var error = (Stream.Video.v1.Sfu.Models.Error)errorProperty.GetValue(response);
            var errorLog = error != null ? $"<color=red>{error.Message}</color>" : "";
            var errorStatus = error != null ? "<color=red>FAILED</color>" : "<color=green>SUCCESS</color>";
            sb.AppendLine($"[RPC Request] {errorStatus} {debugRequestName} | {errorLog}");
            sb.AppendLine(serializedRequest);
            sb.AppendLine();
            sb.AppendLine("Response:");
            sb.AppendLine(serializedResponse);

            _logs.Warning(sb.ToString());
#endif

            return response;
        }

        private bool CanPublish()
        {
            return true;
            //StreamTodo: check if can publish audio/video + observce if we can't and create publisher once it changes
            // val canPublish =
            //     call.state.ownCapabilities.value.any {
            //     it == OwnCapability.SendAudio || it == OwnCapability.SendVideo
            // }
        }

        /**
     * https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/negotiationneeded_event
     *
     * Is called whenever a negotiation is needed. Common examples include
     * - Adding a new media stream
     * - Adding an audio Stream
     * - A screenshare track is started
     *
     * Creates a new SDP
     * - And sets it on the localDescription
     * - Enables video simulcast
     * - calls setPublisher
     * - sets setRemoteDescription
     *
     * Retry behaviour is to retry 3 times quickly as long as
     * - the sfu didn't change
     * - the sdp didn't change
     * If that fails ask the call monitor to do an ice restart
     */
        private void OnNegotiationNeeded()
        {
            //This is called for publisher only

            //create local offer
            //SetLocalDescription
            //SetPublisherRequest <- only when JoinEventResponse was received -> State == Joined

            // getPublisherTracks()
            //SetPublisherRequest
            //setRemoteDescription (from SetPublisherResponse)
        }

        private void OnIceTrickled(RTCIceCandidate iceCandidate, StreamPeerType peerType)
        {
            SendIceCandidateAsync(iceCandidate, peerType).LogIfFailed();
        }

        private void OnSubscriberStreamAdded(MediaStream mediaStream)
        {
            var idParts = mediaStream.Id.Split(":");
            var trackPrefix = idParts[0];
            var trackTypeKey = idParts[1];
            
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"Subscriber stream received, trackPrefix: {trackPrefix}, trackTypeKey: {trackTypeKey}");
#endif

            var participant = _activeCall.Participants.SingleOrDefault(p => p.TrackLookupPrefix == trackPrefix);
            if (participant == null)
            {
                //StreamTodo: figure out severity of this case. Perhaps it's not an error, maybe we haven't received coordinator event yet like ParticipantJoined
                _logs.Warning($"Failed to find participant with trackPrefix: {trackPrefix} for media stream with ID: {mediaStream.Id}");
                return;
            }


            if (!TrackTypeExt.TryGetTrackType(trackTypeKey, out var trackType))
            {
                _logs.Error($"Failed to get {typeof(TrackType)} for value: {trackTypeKey} on media stream with ID: {mediaStream.Id}");
                return;
            }

            if (trackType == Models.Sfu.TrackType.Unspecified)
            {
                _logs.Error($"Unexpected {nameof(trackType)} of value: {trackType} on media stream with ID: {mediaStream.Id}");
                return;
            }
            
            //StreamTodo: assert that we expect exactly one track per type.
            //In theory stream can contain multiple tracks but we're extracting track type from stream ID so I assume it always has to be exactly one track

            foreach (var track in mediaStream.GetAudioTracks())
            {
                //StreamTodo: verify why this is needed. Taken from Android SDK
                track.Enabled = true;
            }
            
            var internalParticipant = ((StreamVideoCallParticipant)participant);

            foreach (var track in mediaStream.GetTracks())
            {
                internalParticipant.SetTrack(trackType, track, out var streamTrack);
                TrackAdded?.Invoke(internalParticipant, streamTrack);
            }
        }

        private void CreateSubscriber(IEnumerable<ICEServer> iceServers)
        {
            _subscriber = new StreamPeerConnection(_logs, StreamPeerType.Subscriber, iceServers);
            _subscriber.IceTrickled += OnIceTrickled;
            _subscriber.StreamAdded += OnSubscriberStreamAdded;
        }

        private void DisposeSubscriber()
        {
            if (_subscriber != null)
            {
                _subscriber.IceTrickled -= OnIceTrickled;
                _subscriber.StreamAdded -= OnSubscriberStreamAdded;
                _subscriber.Dispose();
                _subscriber = null;
            }
        }

        private void CreatePublisher(IEnumerable<ICEServer> iceServers)
        {
            _publisher = new StreamPeerConnection(_logs, StreamPeerType.Publisher, iceServers);
            _publisher.IceTrickled += OnIceTrickled;
            _publisher.NegotiationNeeded += OnNegotiationNeeded;
        }

        private void DisposePublisher()
        {
            if (_publisher != null)
            {
                _publisher.IceTrickled -= OnIceTrickled;
                _publisher.NegotiationNeeded -= OnNegotiationNeeded;
                _publisher.Dispose();
                _publisher = null;
            }
        }
    }
}