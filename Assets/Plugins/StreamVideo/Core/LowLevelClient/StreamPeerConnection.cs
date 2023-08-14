using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public event Action NegotiationNeeded;
        public event Action<RTCIceCandidate, StreamPeerType> IceTrickled;

        public bool IsRemoteDescriptionAvailable
        {
            get
            {
                try
                {
                    // Throws exception if not set
                    return !string.IsNullOrEmpty(_peerConnection.CurrentRemoteDescription.sdp);
                }
                catch
                {
                    return false;
                }
            }
        }

        public StreamPeerConnection(ILogs logs, StreamPeerType peerType)
        {
            _peerType = peerType;
            _logs = logs;

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
                            //StreamTodo: move to config
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

            //StreamTodo: is data channel needed? 
            _sendChannel = _peerConnection.CreateDataChannel("sendChannel");

            _sendChannel.OnOpen += OnSendChannelStatusChanged;
            _sendChannel.OnClose += OnSendChannelStatusChanged;
            _sendChannel.OnMessage += OnSendChannelMessage;

            _videoTransceiver = _peerConnection.AddTransceiver(TrackKind.Video);
            _audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio);
        }

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();

        public void RestartIce() => _peerConnection.RestartIce();

        public Task SetLocalDescriptionAsync(ref RTCSessionDescription offer)
            => _peerConnection.SetLocalDescriptionAsync(ref offer);
        
        public async Task SetRemoteDescriptionAsync(RTCSessionDescription offer)
        {
            await _peerConnection.SetRemoteDescriptionAsync(ref offer);

            _logs.Warning("-------------------Add pending ICE Candidates: " + _pendingIceCandidates.Count);
            
            foreach (var iceCandidate in _pendingIceCandidates)
            {
                _peerConnection.AddIceCandidate(iceCandidate);
            }
        }

        public void AddIceCandidate(RTCIceCandidateInit iceCandidateInit)
        {
            _logs.Warning("---------------------Tried to add ICE Candidate, remote available: " + IsRemoteDescriptionAvailable);
            var iceCandidate = new RTCIceCandidate(iceCandidateInit);
            if (!IsRemoteDescriptionAvailable)
            {
                _pendingIceCandidates.Add(iceCandidate);
                return;
            }

            _peerConnection.AddIceCandidate(iceCandidate);
        }

        public Task<RTCSessionDescription> CreateOfferAsync() => _peerConnection.CreateOfferAsync();

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

        private readonly RTCPeerConnection _peerConnection;
        private readonly RTCDataChannel _sendChannel;
        private readonly RTCRtpTransceiver _videoTransceiver;
        private readonly RTCRtpTransceiver _audioTransceiver;
        private readonly ILogs _logs;
        private readonly StreamPeerType _peerType;
        private VideoStreamTrack _videoStreamTrack;

        private void OnIceCandidate(RTCIceCandidate candidate) => IceTrickled?.Invoke(candidate, _peerType);

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            _logs.Warning("$$$$$$$ OnIceConnectionChange" + _peerType);
        }

        private void OnNegotiationNeeded()
        {
            _logs.Warning("$$$$$$$ OnNegotiationNeeded" + _peerType);

            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations

            NegotiationNeeded?.Invoke();
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            _logs.Warning("$$$$$$$ OnConnectionStateChange" + _peerType);
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            _logs.Warning($"$$$$$$$ OnTrack {_peerType} {trackEvent.Track.GetType()}");

            switch (trackEvent.Track)
            {
                case AudioStreamTrack audioStreamTrack:
                    break;
                case VideoStreamTrack videoStreamTrack:

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
            _logs.Warning("--------------------------------------------------VIDEO RECEIVED");
        }

        private void OnDataChannel(RTCDataChannel channel)
        {
            _logs.Warning("$$$$$$$ OnDataChannel" + _peerType);
        }

        private void OnSendChannelStatusChanged()
        {
            _logs.Warning("$$$$$$$ OnSendChannelStatusChanged" + _peerType);
        }

        private void OnSendChannelMessage(byte[] bytes)
        {
            _logs.Warning("$$$$$$$ OnSendChannelMessage" + _peerType);
        }
    }
}