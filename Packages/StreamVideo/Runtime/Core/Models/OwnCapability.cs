using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Models
{
    /// <summary>
    /// Represents permission capability of a user
    /// </summary>
    public readonly struct OwnCapability : System.IEquatable<OwnCapability>,
        ILoadableFrom<OwnCapabilityInternalEnumDTO, OwnCapability>, ISavableTo<OwnCapabilityInternalEnumDTO>
    {
        public static readonly OwnCapability BlockUsers = new OwnCapability("block-users");
        public static readonly OwnCapability ChangeMaxDuration = new OwnCapability("change-max-duration");
        public static readonly OwnCapability CreateCall = new OwnCapability("create-call");
        public static readonly OwnCapability CreateReaction = new OwnCapability("create-reaction");
        public static readonly OwnCapability EnableNoiseCancellation = new OwnCapability("enable-noise-cancellation");
        public static readonly OwnCapability EndCall = new OwnCapability("end-call");
        public static readonly OwnCapability JoinBackstage = new OwnCapability("join-backstage");
        public static readonly OwnCapability JoinCall = new OwnCapability("join-call");
        public static readonly OwnCapability JoinEndedCall = new OwnCapability("join-ended-call");
        public static readonly OwnCapability MuteUsers = new OwnCapability("mute-users");
        public static readonly OwnCapability PinForEveryone = new OwnCapability("pin-for-everyone");
        public static readonly OwnCapability ReadCall = new OwnCapability("read-call");
        public static readonly OwnCapability RemoveCallMember = new OwnCapability("remove-call-member");
        public static readonly OwnCapability ScreenShare = new OwnCapability("screenshare");
        public static readonly OwnCapability SendAudio = new OwnCapability("send-audio");
        public static readonly OwnCapability SendVideo = new OwnCapability("send-video");
        public static readonly OwnCapability StartBroadcastCall = new OwnCapability("start-broadcast-call");
        public static readonly OwnCapability StartClosedCaptionsCall = new OwnCapability("start-closed-captions-call");
        public static readonly OwnCapability StartFrameRecordCall = new OwnCapability("start-frame-record-call");
        public static readonly OwnCapability StartRecordCall = new OwnCapability("start-record-call");
        public static readonly OwnCapability StartTranscriptionCall = new OwnCapability("start-transcription-call");
        public static readonly OwnCapability StopBroadcastCall = new OwnCapability("stop-broadcast-call");
        public static readonly OwnCapability StopClosedCaptionsCall = new OwnCapability("stop-closed-captions-call");
        public static readonly OwnCapability StopFrameRecordCall = new OwnCapability("stop-frame-record-call");
        public static readonly OwnCapability StopRecordCall = new OwnCapability("stop-record-call");
        public static readonly OwnCapability StopTranscriptionCall = new OwnCapability("stop-transcription-call");
        public static readonly OwnCapability UpdateCall = new OwnCapability("update-call");
        public static readonly OwnCapability UpdateCallMember = new OwnCapability("update-call-member");
        public static readonly OwnCapability UpdateCallPermissions = new OwnCapability("update-call-permissions");
        public static readonly OwnCapability UpdateCallSettings = new OwnCapability("update-call-settings");

        public OwnCapability(string value)
        {
            _value = value;
        }

        public override string ToString() => _value;

        public bool Equals(OwnCapability other) => _value == other._value;

        public override bool Equals(object obj) => obj is OwnCapability other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(OwnCapability left, OwnCapability right) => left.Equals(right);

        public static bool operator !=(OwnCapability left, OwnCapability right) => !left.Equals(right);

        public static implicit operator OwnCapability(string value) => new OwnCapability(value);

        public static implicit operator string(OwnCapability type) => type._value;

        OwnCapability ILoadableFrom<OwnCapabilityInternalEnumDTO, OwnCapability>.
            LoadFromDto(OwnCapabilityInternalEnumDTO dto)
            => new OwnCapability(dto.ToString());

        OwnCapabilityInternalEnumDTO ISavableTo<OwnCapabilityInternalEnumDTO>.SaveToDto()
            => new OwnCapabilityInternalEnumDTO(_value);

        private readonly string _value;
    }
}