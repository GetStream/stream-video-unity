using System;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.Models
{
    public enum OwnCapability
    {
        BlockUsers = 0,
        CreateCall = 1,
        CreateReaction = 2,
        EndCall = 3,
        JoinBackstage = 4,
        JoinCall = 5,
        JoinEndedCall = 6,
        MuteUsers = 7,
        PinForEveryone = 8,
        ReadCall = 9,
        RemoveCallMember = 10,
        Screenshare = 11,
        SendAudio = 12,
        SendVideo = 13,
        StartBroadcastCall = 14,
        StartRecordCall = 15,
        StartTranscriptionCall = 16,
        StopBroadcastCall = 17,
        StopRecordCall = 18,
        StopTranscriptionCall = 19,
        UpdateCall = 20,
        UpdateCallMember = 21,
        UpdateCallPermissions = 22,
        UpdateCallSettings = 23,
        EnableNoiseCancellation = 24,
        ChangeMaxDuration = 25,
    }

    internal static class OwnCapabilityExt
    {
        public static OwnCapabilityInternalEnum ToInternalEnum(this OwnCapability ownCapability)
        {
            switch (ownCapability)
            {
                case OwnCapability.BlockUsers: return OwnCapabilityInternalEnum.BlockUsers;
                case OwnCapability.CreateCall: return OwnCapabilityInternalEnum.CreateCall;
                case OwnCapability.CreateReaction: return OwnCapabilityInternalEnum.CreateReaction;
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
                case OwnCapability.StartRecordCall: return OwnCapabilityInternalEnum.StartRecordCall;
                case OwnCapability.StartTranscriptionCall: return OwnCapabilityInternalEnum.StartTranscriptionCall;
                case OwnCapability.StopBroadcastCall: return OwnCapabilityInternalEnum.StopBroadcastCall;
                case OwnCapability.StopRecordCall: return OwnCapabilityInternalEnum.StopRecordCall;
                case OwnCapability.StopTranscriptionCall: return OwnCapabilityInternalEnum.StopTranscriptionCall;
                case OwnCapability.UpdateCall: return OwnCapabilityInternalEnum.UpdateCall;
                case OwnCapability.UpdateCallMember: return OwnCapabilityInternalEnum.UpdateCallMember;
                case OwnCapability.UpdateCallPermissions: return OwnCapabilityInternalEnum.UpdateCallPermissions;
                case OwnCapability.UpdateCallSettings: return OwnCapabilityInternalEnum.UpdateCallSettings;
                case OwnCapability.EnableNoiseCancellation: return OwnCapabilityInternalEnum.EnableNoiseCancellation;
                case OwnCapability.ChangeMaxDuration: return OwnCapabilityInternalEnum.ChangeMaxDuration;
                default: throw new ArgumentOutOfRangeException(nameof(ownCapability), ownCapability, null);
            }
        }

        public static OwnCapability ToPublicEnum(this OwnCapabilityInternalEnum ownCapability)
        {
            switch (ownCapability)
            {
                case OwnCapabilityInternalEnum.BlockUsers: return OwnCapability.BlockUsers;
                case OwnCapabilityInternalEnum.CreateCall: return OwnCapability.CreateCall;
                case OwnCapabilityInternalEnum.CreateReaction: return OwnCapability.CreateReaction;
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
                case OwnCapabilityInternalEnum.StartRecordCall: return OwnCapability.StartRecordCall;
                case OwnCapabilityInternalEnum.StartTranscriptionCall: return OwnCapability.StartTranscriptionCall;
                case OwnCapabilityInternalEnum.StopBroadcastCall: return OwnCapability.StopBroadcastCall;
                case OwnCapabilityInternalEnum.StopRecordCall: return OwnCapability.StopRecordCall;
                case OwnCapabilityInternalEnum.StopTranscriptionCall: return OwnCapability.StopTranscriptionCall;
                case OwnCapabilityInternalEnum.UpdateCall: return OwnCapability.UpdateCall;
                case OwnCapabilityInternalEnum.UpdateCallMember: return OwnCapability.UpdateCallMember;
                case OwnCapabilityInternalEnum.UpdateCallPermissions: return OwnCapability.UpdateCallPermissions;
                case OwnCapabilityInternalEnum.UpdateCallSettings: return OwnCapability.UpdateCallSettings;
                case OwnCapabilityInternalEnum.EnableNoiseCancellation: return OwnCapability.EnableNoiseCancellation;
                case OwnCapabilityInternalEnum.ChangeMaxDuration: return OwnCapability.ChangeMaxDuration;
                default: throw new ArgumentOutOfRangeException(nameof(ownCapability), ownCapability, null);
            }
        }
    }
}