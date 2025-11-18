namespace StreamVideo.Core.Trace
{
    /// <summary>
    /// Trace keys for peer connection events and SFU operations.
    /// </summary>
    internal static class PeerConnectionTraceKey
    {
        public const string Create = "create";
        public const string JoinRequest = "joinRequest";
        public const string OnIceCandidate = "onicecandidate";
        public const string OnTrack = "ontrack";
        public const string OnSignalingStateChange = "onsignalingstatechange";
        public const string OnIceConnectionStateChange = "oniceconnectionstatechange";
        public const string OnIceGatheringStateChange = "onicegatheringstatechange";
        public const string OnConnectionStateChange = "onconnectionstatechange";
        public const string OnNegotiationNeeded = "onnegotiationneeded";
        public const string OnDataChannel = "ondatachannel";
        public const string Close = "close";
        public const string CreateOffer = "createOffer";
        public const string CreateAnswer = "createAnswer";
        public const string SetLocalDescription = "setLocalDescription";
        public const string SetRemoteDescription = "setRemoteDescription";
        public const string AddIceCandidate = "addIceCandidate";
        public const string ChangePublishQuality = "changePublishQuality";
        public const string ChangePublishOptions = "changePublishOptions";
        public const string GoAway = "goAway";
        public const string SfuError = "error";
        public const string CallEnded = "callEnded";
        
        // Negotiation error traces
        public const string NegotiateErrorSetLocalDescription = "negotiate-error-setlocaldescription";
        public const string NegotiateErrorSetRemoteDescription = "negotiate-error-setremotedescription";
        public const string NegotiateErrorSetPublisher = "negotiate-error-setpublisher";
        public const string NegotiateErrorSendAnswer = "negotiate-error-sendanswer";
        public const string NegotiateErrorSubmit = "negotiate-error-submit";
        public const string NegotiateWithTracks = "negotiate-with-tracks";
        
        // ICE and reconnect traces
        public const string IceRestartError = "iceRestart-error";
        public const string FastReconnect = "fastReconnect";
    }
}

