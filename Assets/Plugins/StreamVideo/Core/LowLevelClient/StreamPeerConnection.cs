using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core.Models;
using StreamVideo.Libs.Logs;
using Unity.WebRTC;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Wrapper around WebRTC Peer Connection instance
    /// </summary>
    internal class StreamPeerConnection : IDisposable
    {
        //StreamTodo: for debug only, figure this out better
        public Action<Texture> VideoReceived;

        public event Action NegotiationNeeded;
        public event Action<RTCIceCandidate, StreamPeerType> IceTrickled;
        
        public MediaStream ReceiveStream { get; private set; }

        public bool IsRemoteDescriptionAvailable
        {
            get
            {
                try
                {
                    // Throws exception if not set
                    return !string.IsNullOrEmpty(_peerConnection.RemoteDescription.sdp);
                }
                catch
                {
                    return false;
                }
            }
        }

        public StreamPeerConnection(ILogs logs, StreamPeerType peerType, IEnumerable<ICEServer> iceServers)
        {
            _peerType = peerType;
            _logs = logs;

            var rtcIceServers = new List<RTCIceServer>();

            foreach (var ice in iceServers)
            {
                rtcIceServers.Add(new RTCIceServer
                {
                    credential = ice.Password,
                    credentialType = RTCIceCredentialType.Password,
                    urls = ice.Urls.ToArray(),
                    username = ice.Username
                });
            }

            var conf = new RTCConfiguration
            {
                iceServers = rtcIceServers.ToArray(),
                iceTransportPolicy = RTCIceTransportPolicy.Relay,
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

            //StreamTodo: is data channel needed? 
            _sendChannel = _peerConnection.CreateDataChannel("sendChannel");

            _sendChannel.OnOpen += OnSendChannelStatusChanged;
            _sendChannel.OnClose += OnSendChannelStatusChanged;
            _sendChannel.OnMessage += OnSendChannelMessage;

            ReceiveStream = new MediaStream();

            var direction = _peerType == StreamPeerType.Publisher
                ? RTCRtpTransceiverDirection.SendOnly
                : RTCRtpTransceiverDirection.RecvOnly;
            //StreamTodo: review adding transceivers (probably needed to disable/enable tracks during session)
            // _videoTransceiver = _peerConnection.AddTransceiver(TrackKind.Video, new RTCRtpTransceiverInit
            // {
            //     direction = direction,
            //     // sendEncodings = new RTCRtpEncodingParameters[]
            //     // {
            //     // },
            //     streams = new MediaStream[]
            //     {
            //         ReceiveStream
            //     }
            // });
            //_audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio);
        }

        public void RestartIce() => _peerConnection.RestartIce();

        public Task SetLocalDescriptionAsync(ref RTCSessionDescription offer)
        {
            _logs.Warning($"------------------- [{_peerType}] Set LocalDesc:\n" + offer.sdp);
            return _peerConnection.SetLocalDescriptionAsync(ref offer);
        }

        public async Task SetRemoteDescriptionAsync(RTCSessionDescription offer)
        {
            await _peerConnection.SetRemoteDescriptionAsync(ref offer);

            _logs.Warning(
                $"------------------- [{_peerType}] Set RemoteDesc & send pending ICE Candidates: {_pendingIceCandidates.Count}, IsRemoteDescriptionAvailable: {IsRemoteDescriptionAvailable}, offer:\n{offer.sdp}");

            foreach (var iceCandidate in _pendingIceCandidates)
            {
                _peerConnection.AddIceCandidate(iceCandidate);
            }
        }

        public void AddIceCandidate(RTCIceCandidateInit iceCandidateInit)
        {
            _logs.Warning($"---------------------[{_peerType}] Add ICE Candidate, remote available: {IsRemoteDescriptionAvailable}, candidate: {iceCandidateInit.candidate}");
            var iceCandidate = new RTCIceCandidate(iceCandidateInit);
            if (!IsRemoteDescriptionAvailable)
            {
                _pendingIceCandidates.Add(iceCandidate);
                return;
            }

            _peerConnection.AddIceCandidate(iceCandidate);
        }

        public Task<RTCSessionDescription> CreateOfferAsync() => _peerConnection.CreateOfferAsync();
        
        public Task<RTCSessionDescription> CreateAnswerAsync() => _peerConnection.CreateAnswerAsync();

        public void Dispose()
        {
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
            if (ReceiveStream == null)
            {
                return;
            }
            Texture prevTexture = null;
            IntPtr prevPointer = default;
            var count = ReceiveStream.GetVideoTracks().Count();
            foreach (var track in ReceiveStream.GetVideoTracks())
            {
                if (prevTexture != track.Texture)
                {
                    var hasCodeChanged = prevTexture?.GetHashCode() != track.Texture.GetHashCode();
                    _logs.Warning($"%%%%%%%%%%%%%%%%%%%%%%%%%%% Texture Changed; {count}, hashCodeChanged: {hasCodeChanged}");
                    prevTexture = track.Texture;
                    
                    VideoReceived?.Invoke(track.Texture);
                }
                if (prevPointer != track.TexturePtr)
                {
                    _logs.Warning($"%%%%%%%%%%%%%%%%%%%%%%%%%%% TexturePtr Changed; {count}");
                    prevPointer = track.TexturePtr;
                }
            }
        }

        private readonly RTCPeerConnection _peerConnection;
        private readonly RTCDataChannel _sendChannel;
        private readonly RTCRtpTransceiver _videoTransceiver;
        private readonly RTCRtpTransceiver _audioTransceiver;
        private readonly ILogs _logs;
        private readonly StreamPeerType _peerType;

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();

        private VideoStreamTrack _videoStreamTrack;

        private void OnIceCandidate(RTCIceCandidate candidate) => IceTrickled?.Invoke(candidate, _peerType);

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnIceConnectionChange to: " + state);
        }

        private void OnNegotiationNeeded()
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnNegotiationNeeded");

            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations

            NegotiationNeeded?.Invoke();
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnConnectionStateChange to: {state}");
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnTrack {trackEvent.Track.GetType()}");

            switch (trackEvent.Track)
            {
                case AudioStreamTrack audioStreamTrack:
                    break;
                case VideoStreamTrack videoStreamTrack:

                    ReceiveStream = trackEvent.Streams.First();

                    //StreamTodo: handle receiving it again + cleanup (unsubscribe)
                    _videoStreamTrack = videoStreamTrack;
                    _videoStreamTrack.OnVideoReceived += OnVideoReceived;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnVideoReceived(Texture renderer)
        {
            _logs.Warning($"[{_peerType}] --------------------------------------------------VIDEO RECEIVED");
            
            VideoReceived?.Invoke(renderer);
        }

        private void OnDataChannel(RTCDataChannel channel)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnDataChannel");
        }

        private void OnSendChannelStatusChanged()
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnSendChannelStatusChanged");
        }

        private void OnSendChannelMessage(byte[] bytes)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnSendChannelMessage");
        }
    }
}