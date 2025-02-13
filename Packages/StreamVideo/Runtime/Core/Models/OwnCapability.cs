using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum OwnCapability
    {
        BlockUsers = 0,
        ChangeMaxDuration = 1,
        CreateCall = 2,
        CreateReaction = 3,
        EnableNoiseCancellation = 4,
        EndCall = 5,
        JoinBackstage = 6,
        JoinCall = 7,
        JoinEndedCall = 8,
        MuteUsers = 9,
        PinForEveryone = 10,
        ReadCall = 11,
        RemoveCallMember = 12,
        Screenshare = 13,
        SendAudio = 14,
        SendVideo = 15,
        StartBroadcastCall = 16,
        StartClosedCaptionsCall = 17,
        StartFrameRecordCall = 18,
        StartRecordCall = 19,
        StartTranscriptionCall = 20,
        StopBroadcastCall = 21,
        StopClosedCaptionsCall = 22,
        StopFrameRecordCall = 23,
        StopRecordCall = 24,
        StopTranscriptionCall = 25,
        UpdateCall = 26,
        UpdateCallMember = 27,
        UpdateCallPermissions = 28,
        UpdateCallSettings = 29,
    }

    internal static class OwnCapabilityExt
    {
        public static OwnCapabilityInternalEnum ToInternalEnum(this OwnCapability ownCapability)
        {
            switch (ownCapability)
            {
                case OwnCapability.BlockUsers: return OwnCapabilityInternalEnum.BlockUsers;
                case OwnCapability.ChangeMaxDuration: return OwnCapabilityInternalEnum.ChangeMaxDuration;
                case OwnCapability.CreateCall: return OwnCapabilityInternalEnum.CreateCall;
                case OwnCapability.CreateReaction: return OwnCapabilityInternalEnum.CreateReaction;
                case OwnCapability.EnableNoiseCancellation: return OwnCapabilityInternalEnum.EnableNoiseCancellation;
                case OwnCapability.EndCall: return OwnCapabilityInternalEnum.EndCall;
                case OwnCapability.JoinBackstage: return OwnCapabilityInternalEnum.JoinBackstage;
                case OwnCapability.JoinCall: return OwnCapabilityInternalEnum.JoinCall;
                case OwnCapability.JoinEndedCall: return OwnCapabilityInternalEnum.JoinEndedCall;
                case OwnCapability.MuteUsers: return OwnCapabilityInternalEnum.MuteUsers;
                case OwnCapability.PinForEveryone: return OwnCapabilityInternalEnum.PinForEveryone;
                case OwnCapability.ReadCall: return OwnCapabilityInternalEnum.ReadCall;
                case OwnCapability.RemoveCallMember: return OwnCapabilityInternalEnum.RemoveCallMember;
                case OwnCapability.Screenshare: return OwnCapabilityInternalEnum.Screenshare;
                case OwnCapability.SendAudio: return OwnCapabilityInternalEnum.SendAudio;
                case OwnCapability.SendVideo: return OwnCapabilityInternalEnum.SendVideo;
                case OwnCapability.StartBroadcastCall: return OwnCapabilityInternalEnum.StartBroadcastCall;
                case OwnCapability.StartClosedCaptionsCall: return OwnCapabilityInternalEnum.StartClosedCaptionsCall;
                case OwnCapability.StartFrameRecordCall: return OwnCapabilityInternalEnum.StartFrameRecordCall;
                case OwnCapability.StartRecordCall: return OwnCapabilityInternalEnum.StartRecordCall;
                case OwnCapability.StartTranscriptionCall: return OwnCapabilityInternalEnum.StartTranscriptionCall;
                case OwnCapability.StopBroadcastCall: return OwnCapabilityInternalEnum.StopBroadcastCall;
                case OwnCapability.StopClosedCaptionsCall: return OwnCapabilityInternalEnum.StopClosedCaptionsCall;
                case OwnCapability.StopFrameRecordCall: return OwnCapabilityInternalEnum.StopFrameRecordCall;
                case OwnCapability.StopRecordCall: return OwnCapabilityInternalEnum.StopRecordCall;
                case OwnCapability.StopTranscriptionCall: return OwnCapabilityInternalEnum.StopTranscriptionCall;
                case OwnCapability.UpdateCall: return OwnCapabilityInternalEnum.UpdateCall;
                case OwnCapability.UpdateCallMember: return OwnCapabilityInternalEnum.UpdateCallMember;
                case OwnCapability.UpdateCallPermissions: return OwnCapabilityInternalEnum.UpdateCallPermissions;
                case OwnCapability.UpdateCallSettings: return OwnCapabilityInternalEnum.UpdateCallSettings;
                default: throw new ArgumentOutOfRangeException(nameof(ownCapability), ownCapability, null);
            }
        }

        public static OwnCapability ToPublicEnum(this OwnCapabilityInternalEnum ownCapability)
        {
            switch (ownCapability)
            {
                case OwnCapabilityInternalEnum.BlockUsers: return OwnCapability.BlockUsers;
                case OwnCapabilityInternalEnum.ChangeMaxDuration: return OwnCapability.ChangeMaxDuration;
                case OwnCapabilityInternalEnum.CreateCall: return OwnCapability.CreateCall;
                case OwnCapabilityInternalEnum.CreateReaction: return OwnCapability.CreateReaction;
                case OwnCapabilityInternalEnum.EnableNoiseCancellation: return OwnCapability.EnableNoiseCancellation;
                case OwnCapabilityInternalEnum.EndCall: return OwnCapability.EndCall;
                case OwnCapabilityInternalEnum.JoinBackstage: return OwnCapability.JoinBackstage;
                case OwnCapabilityInternalEnum.JoinCall: return OwnCapability.JoinCall;
                case OwnCapabilityInternalEnum.JoinEndedCall: return OwnCapability.JoinEndedCall;
                case OwnCapabilityInternalEnum.MuteUsers: return OwnCapability.MuteUsers;
                case OwnCapabilityInternalEnum.PinForEveryone: return OwnCapability.PinForEveryone;
                case OwnCapabilityInternalEnum.ReadCall: return OwnCapability.ReadCall;
                case OwnCapabilityInternalEnum.RemoveCallMember: return OwnCapability.RemoveCallMember;
                case OwnCapabilityInternalEnum.Screenshare: return OwnCapability.Screenshare;
                case OwnCapabilityInternalEnum.SendAudio: return OwnCapability.SendAudio;
                case OwnCapabilityInternalEnum.SendVideo: return OwnCapability.SendVideo;
                case OwnCapabilityInternalEnum.StartBroadcastCall: return OwnCapability.StartBroadcastCall;
                case OwnCapabilityInternalEnum.StartClosedCaptionsCall: return OwnCapability.StartClosedCaptionsCall;
                case OwnCapabilityInternalEnum.StartFrameRecordCall: return OwnCapability.StartFrameRecordCall;
                case OwnCapabilityInternalEnum.StartRecordCall: return OwnCapability.StartRecordCall;
                case OwnCapabilityInternalEnum.StartTranscriptionCall: return OwnCapability.StartTranscriptionCall;
                case OwnCapabilityInternalEnum.StopBroadcastCall: return OwnCapability.StopBroadcastCall;
                case OwnCapabilityInternalEnum.StopClosedCaptionsCall: return OwnCapability.StopClosedCaptionsCall;
                case OwnCapabilityInternalEnum.StopFrameRecordCall: return OwnCapability.StopFrameRecordCall;
                case OwnCapabilityInternalEnum.StopRecordCall: return OwnCapability.StopRecordCall;
                case OwnCapabilityInternalEnum.StopTranscriptionCall: return OwnCapability.StopTranscriptionCall;
                case OwnCapabilityInternalEnum.UpdateCall: return OwnCapability.UpdateCall;
                case OwnCapabilityInternalEnum.UpdateCallMember: return OwnCapability.UpdateCallMember;
                case OwnCapabilityInternalEnum.UpdateCallPermissions: return OwnCapability.UpdateCallPermissions;
                case OwnCapabilityInternalEnum.UpdateCallSettings: return OwnCapability.UpdateCallSettings;
                default: throw new ArgumentOutOfRangeException(nameof(ownCapability), ownCapability, null);
            }
        }
    }
}