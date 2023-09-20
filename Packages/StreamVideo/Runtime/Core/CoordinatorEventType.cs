namespace StreamVideo.Core
{
    internal static class CoordinatorEventType
    {
        public const string CallAccepted = "call.accepted";
        public const string CallBlockedUser = "call.blocked_user";
        public const string CallBroadcastingStarted = "call.broadcasting_started";
        public const string CallBroadcastingStopped = "call.broadcasting_stopped";
        public const string CallCreated = "call.created";
        public const string CallEnded = "call.ended";
        public const string CallLiveStarted = "call.live_started";
        public const string CallMemberAdded = "call.member_added";
        public const string CallMemberRemoved = "call.member_removed";
        public const string CallMemberUpdated = "call.member_updated";
        public const string CallMemberUpdatedPermission = "call.member_updated_permission";
        public const string CallNotification = "call.notification";
        public const string CallPermissionRequest = "call.permission_request";
        public const string CallPermissionsUpdated = "call.permissions_updated";
        public const string CallReactionNew = "call.reaction_new";
        public const string CallRecordingStarted = "call.recording_started";
        public const string CallRecordingStopped = "call.recording_stopped";
        public const string CallRejected = "call.rejected";
        public const string CallRing = "call.ring";
        public const string CallSessionEnded = "call.session_ended";
        public const string CallSessionParticipantJoined = "call.session_participant_joined";
        public const string CallSessionParticipantLeft = "call.session_participant_left";
        public const string CallSessionStarted = "call.session_started";
        public const string CallUnblockedUser = "call.unblocked_user";
        public const string CallUpdated = "call.updated";
        public const string ConnectionError = "connection.error";
        public const string ConnectionOk = "connection.ok";
        public const string Custom = "custom";
        public const string HealthCheck = "health.check";
    }
}