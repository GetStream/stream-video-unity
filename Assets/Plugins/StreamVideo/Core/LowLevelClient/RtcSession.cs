using System.Threading.Tasks;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    internal class RtcSession
    {
        public RTCSessionDescription Offer { get; private set; }

        public RtcSession()
        {
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

        //StreamTodo: not obvious that init creates an offer
        public async Task InitAsync()
        {
            Offer = await _peerConnection.CreateOfferAsync();
            
        }
        
        private readonly RTCPeerConnection _peerConnection;
        private readonly RTCDataChannel _sendChannel;
        private readonly RTCRtpTransceiver _videoTransceiver;
        private readonly RTCRtpTransceiver _audioTransceiver;

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