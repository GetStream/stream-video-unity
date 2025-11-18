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
    }
}

