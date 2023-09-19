using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.QueryBuilders.Sort;
using StreamVideo.Core.State;

namespace StreamVideo.Core.StatefulModels
{
    public interface IStreamCall : IStreamStatefulModel
    {
        event ParticipantTrackChangedHandler TrackAdded;

        Credentials Credentials { get; }
        IReadOnlyList<IStreamVideoCallParticipant> Participants { get; }
        IReadOnlyList<OwnCapability> OwnCapabilities { get; }

        /// <summary>
        /// Call ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The unique identifier for a call (&lt;type&gt;:&lt;id&gt;)
        /// </summary>
        string Cid { get; }

        /// <summary>
        /// The type of call
        /// </summary>
        StreamCallType Type { get; }

        /// <summary>
        /// The user that created the call
        /// </summary>
        IStreamVideoUser CreatedBy { get; }

        bool IsLocalUserOwner { get; }

        event ParticipantJoinedHandler ParticipantJoined;
        event ParticipantLeftHandler ParticipantLeft;

        Task LeaveAsync();

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
    }
}