using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.QueryBuilders.Sort;
using StreamVideo.Core.State;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// Represents a call session where participants can share audio and video streams.
    /// </summary>
    public interface IStreamCall : IStreamStatefulModel, IHasCustomData
    {
        /// <summary>
        /// A new participant joined the call
        /// </summary>
        event ParticipantJoinedHandler ParticipantJoined;
        
        /// <summary>
        /// A participant left the call
        /// </summary>
        event ParticipantLeftHandler ParticipantLeft;
        
        /// <summary>
        /// A track was added for a participant in the call
        /// </summary>
        event ParticipantTrackChangedHandler ParticipantTrackAdded;

        /// <summary>
        /// The most actively speaking participant in the call has changed
        /// </summary>
        event DominantSpeakerChangedHandler DominantSpeakerChanged;

        /// <summary>
        /// Notifies that a call participant added a reaction to this call
        /// </summary>
        event CallReactionAddedHandler ReactionAdded;
        
        /// <summary>
        /// Notifies that the <see cref="PinnedParticipants"/> collection was updated
        /// </summary>
        event Action PinnedParticipantsUpdated;
        
        /// <summary>
        /// Recording of this call started
        /// </summary>
        event Action RecordingStarted;
        
        /// <summary>
        /// Recording of this call stopped
        /// </summary>
        event Action RecordingStopped;
        
        /// <summary>
        /// Notifies that the <see cref="SortedParticipants"/> collection was updated
        /// </summary>
        event Action SortedParticipantsUpdated;
        
        /// <summary>
        /// Event fired when a call event is received
        /// </summary>
        event Action<CallEvent> EventReceived;
        
        /// <summary>
        /// Event fired when call data was updated
        /// </summary>
        event Action Updated;

        Credentials Credentials { get; }
        
        /// <summary>
        /// Participants are users that are currently in the call. You can also get all users associated with the call via <see cref="Members"/>
        /// </summary>
        IReadOnlyList<IStreamVideoCallParticipant> Participants { get; }

        /// <summary>
        /// Participant that are pinned to this call sorted by the time they were pinned.
        /// Locally pinned participants are first, then the participant pinned remotely (by other participants with appropriate permissions).
        /// Any update to this collection will trigger the <see cref="PinnedParticipantsUpdated"/> event.
        /// </summary>
        IReadOnlyList<IStreamVideoCallParticipant> PinnedParticipants { get; }

        /// <summary>
        /// Participants sorted by:
        /// - anyone who is pinned (locally pinned first, then remotely pinned)
        /// - anyone who is screen-sharing
        /// - dominant speaker
        /// - all other video participants
        /// - audio only participants
        /// Any update to this collection will trigger the <see cref="SortedParticipantsUpdated"/> event.
        /// </summary>
        IEnumerable<IStreamVideoCallParticipant> SortedParticipants { get; }
        
        ParticipantCount ParticipantCount { get; }

        /// <summary>
        /// Participant that is currently the most actively speaking.
        /// When dominant speaker changes the <see cref="DominantSpeakerChanged"/> event will trigger.
        /// You can also get the <see cref="PreviousDominantSpeaker"/>
        /// </summary>
        IStreamVideoCallParticipant DominantSpeaker { get; }

        /// <summary>
        /// Participant that was the last
        /// </summary>
        IStreamVideoCallParticipant PreviousDominantSpeaker { get; }

        IReadOnlyList<OwnCapability> OwnCapabilities { get; }

        /// <summary>
        /// Call ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The unique identifier for a call - this is a combined <see cref="Type"/> and <see cref="Id"/> in format -> type:id
        /// </summary>
        string Cid { get; }

        /// <summary>
        /// The type of call. The type determines the permissions schema used. You can pick from predefined types or create your own in the dashboard (https://dashboard.getstream.io/) 
        /// </summary>
        StreamCallType Type { get; }

        /// <summary>
        /// Does the user of this client own the call
        /// </summary>
        bool IsLocalUserOwner { get; }
        
        /// <summary>
        /// Members are users permanently associated with the call. This includes users who haven't joined the call.
        /// So for example you create a call and invite "Jane", "Peter", and "Steven" but only "Jane" joins the call.
        /// All three will be members but only "Jane" will be a participant. You can get call participants with <see cref="Participants"/>
        /// </summary>
        IEnumerable<CallMember> Members { get; }
        
        /// <summary>
        /// Is the call being currently recorded
        /// </summary>
        bool Recording { get; }
        
        /// <summary>
        /// Users that are blocked in this call
        /// </summary>
        IEnumerable<IStreamVideoUser> BlockedUsers { get; }
        CallSettings Settings { get; }
        bool Backstage { get; }

        /// <summary>
        /// Date/time of creation
        /// </summary>
        DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Date/time of the last update
        /// </summary>
        DateTimeOffset UpdatedAt { get; }

        /// <summary>
        /// Date/time when the call will start
        /// </summary>
        DateTimeOffset StartsAt { get; }

        /// <summary>
        /// Date/time when the call ended
        /// </summary>
        DateTimeOffset EndedAt { get; }

        string Team { get; }

        /// <summary>
        /// The user that created the call
        /// </summary>
        IStreamVideoUser CreatedBy { get; }

        CallIngress Ingress { get; }

        CallEgress Egress { get; }

        /// <summary>
        /// Leave the call without ending it. Other participants will remain connected. If you wish to end the call for all participants you can use the <see cref="EndAsync"/>
        /// </summary>
        Task LeaveAsync();

        /// <summary>
        /// End call for all participants. If you only want to leave the call without ending it for others you can use the <see cref="LeaveAsync"/>
        /// </summary>
        Task EndAsync();

        Task GoLiveAsync();

        Task StopLiveAsync();

        Task StartRecordingAsync();

        Task StopRecordingAsync();

        Task MuteAllUsersAsync(bool audio, bool video, bool screenShare);

        Task BlockUserAsync(string userId);

        Task BlockUserAsync(IStreamVideoUser user);

        Task BlockUserAsync(IStreamVideoCallParticipant participant);

        Task UnblockUserAsync(string userId);

        Task UnblockUserAsync(IStreamVideoUser user);

        Task UnblockUserAsync(IStreamVideoCallParticipant participant);

        /// <summary>
        /// Check if you currently have this permission
        /// </summary>
        /// <param name="permission">Permission to check</param>
        bool HasPermissions(OwnCapability permission);

        /// <summary>
        /// Request call host to grant you this permission
        /// </summary>
        /// <param name="permission">Requested permission</param>
        Task RequestPermissionAsync(OwnCapability permission);

        /// <summary>
        /// Request call host to grant you this permission
        /// </summary>
        /// <param name="permissions">Requested permission</param>
        Task RequestPermissionsAsync(IEnumerable<OwnCapability> permissions);

        /// <summary>
        /// Grant permissions to a user in this call
        /// </summary>
        /// <param name="permissions">Permissions to grant</param>
        /// <param name="userId">User that will receive permissions</param>
        Task GrantPermissionsAsync(IEnumerable<OwnCapability> permissions, string userId);

        Task GrantPermissionsAsync(IEnumerable<OwnCapability> permissions, IStreamVideoUser user);

        Task GrantPermissionsAsync(IEnumerable<OwnCapability> permissions, IStreamVideoCallParticipant participant);

        /// <summary>
        /// Revoke permissions from a user in this call
        /// </summary>
        /// <param name="permissions">Permissions to revoke</param>
        /// <param name="userId">User that will have permissions revoked</param>
        Task RevokePermissionsAsync(IEnumerable<OwnCapability> permissions, string userId);

        Task RevokePermissionsAsync(IEnumerable<OwnCapability> permissions, IStreamVideoUser user);

        Task RevokePermissionsAsync(IEnumerable<OwnCapability> permissions, IStreamVideoCallParticipant participant);

        /// <summary>
        /// Marks the incoming call as accepted.
        /// This method should be used only for "ringing" call flows.
        /// <see cref="StreamCall.JoinAsync"/> invokes this method automatically for you when joining a call.
        /// </summary>
        Task AcceptAsync();

        /// <summary>
        /// Marks the incoming call as rejected.
        /// This method should be used only for "ringing" call flows.
        /// <see cref="StreamCall.LeaveAsync"/> invokes this method automatically for you when you leave or reject this call.
        /// </summary>
        Task RejectAsync();

        Task StartHLS();

        Task StopHLS();

        Task RemoveMembersAsync(IEnumerable<string> userIds);

        Task RemoveMembersAsync(IEnumerable<IStreamVideoUser> users);

        Task RemoveMembersAsync(IEnumerable<IStreamVideoCallParticipant> participants);

        Task MuteUsersAsync(IEnumerable<string> userIds, bool audio, bool video, bool screenShare);

        Task MuteUsersAsync(IEnumerable<IStreamVideoUser> users, bool audio, bool video, bool screenShare);

        Task MuteUsersAsync(IEnumerable<IStreamVideoCallParticipant> participants, bool audio, bool video,
            bool screenShare);
        
        void MuteSelf(bool audio, bool video, bool screenShare);

        void MuteOthers(bool audio, bool video, bool screenShare);

        /// <summary>
        /// Query members in this call. The result won't be stored in the call state.
        /// </summary>
        /// <param name="filters">[Optional] filters</param>
        /// <param name="sort">[Optional] sort</param>
        /// <param name="limit">how many records </param>
        /// <param name="prev">[Optional]</param>
        /// <param name="next">[Optional]</param>
        /// <returns></returns>
        Task<QueryMembersResult> QueryMembersAsync(IEnumerable<IFieldFilterRule> filters = null,
            CallMemberSort sort = null, int limit = 25, string prev = null, string next = null);

        Task SendReactionAsync(string type);

        Task SendReactionAsync(string type, string emojiCode, Dictionary<string, object> customData = null);

        Task SendCustomEventAsync(Dictionary<string, object> eventData);

        /// <summary>
        /// Pin this participant locally. This will take effect on this device only.
        /// You can get all pinned participants with <see cref="StreamCall.PinnedParticipants"/>
        /// </summary>
        /// <param name="participant">Participant to pin</param>
        void PinLocally(IStreamVideoCallParticipant participant);

        /// <summary>
        /// Unpin this participant locally. This will take effect on this device only.
        /// You can get all pinned participants with <see cref="PinnedParticipants"/>
        /// </summary>
        /// <param name="participant">Participant to unpin</param>
        void UnpinLocally(IStreamVideoCallParticipant participant);

        /// <summary>
        /// Check if this participant is pinned locally. Also check <see cref="IsPinnedRemotely"/> & <see cref="IsPinned"/>.
        /// </summary>
        /// <returns>True if participant is pinned locally</returns>
        bool IsPinnedLocally(IStreamVideoCallParticipant participant);

        /// <summary>
        /// Check if this participant is pinned remotely. Also check <see cref="IsPinnedLocally"/> & <see cref="IsPinned"/>.
        /// </summary>
        /// <returns>True if participant is pinned remotely</returns>
        bool IsPinnedRemotely(IStreamVideoCallParticipant participant);

        /// <summary>
        /// Check if this participant is pinned. Also check <see cref="IsPinnedLocally"/> & <see cref="IsPinnedRemotely"/>.
        /// </summary>
        /// <returns>True if participant is pinned remotely</returns>
        bool IsPinned(IStreamVideoCallParticipant participant);
    }
}