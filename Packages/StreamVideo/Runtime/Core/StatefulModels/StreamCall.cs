using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Utils;
using Stream.Video.v1.Sfu.Events;
using Stream.Video.v1.Sfu.Models;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.QueryBuilders.Sort;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core
{
    public delegate void DominantSpeakerChangedHandler(IStreamVideoCallParticipant currentDominantSpeaker,
        IStreamVideoCallParticipant previousDominantSpeaker);

    public delegate void CallReactionAddedHandler(Reaction reaction, IStreamVideoCallParticipant participant);

    /// <summary>
    /// Represents a call during which participants can share: audio, video, screen
    /// </summary>
    internal sealed class StreamCall : StreamStatefulModelBase<StreamCall>,
        IUpdateableFrom<CallResponseInternalDTO, StreamCall>,
        IUpdateableFrom<GetCallResponseInternalDTO, StreamCall>,
        IUpdateableFrom<GetOrCreateCallResponseInternalDTO, StreamCall>,
        IUpdateableFrom<JoinCallResponseInternalDTO, StreamCall>,
        IUpdateableFrom<CallStateResponseFieldsInternalDTO, StreamCall>,
        IStreamCall
    {
        public event ParticipantTrackChangedHandler TrackAdded;

        public event ParticipantJoinedHandler ParticipantJoined;
        public event ParticipantLeftHandler ParticipantLeft;

        public event DominantSpeakerChangedHandler DominantSpeakerChanged;

        public event Action PinnedParticipantsUpdated;

        //public event Action SortedParticipantsUpdated; //StreamTodo: implement

        public event CallReactionAddedHandler ReactionAdded;

        public event Action RecordingStarted;
        public event Action RecordingStopped;

        public IReadOnlyList<IStreamVideoCallParticipant> Participants => Session.Participants;

        public bool IsLocalUserOwner
        {
            get
            {
                var localParticipant = Participants.FirstOrDefault(p => p.IsLocalParticipant);
                return CreatedBy.Id == localParticipant?.UserId;
            }
        }

        public IStreamVideoCallParticipant DominantSpeaker
        {
            get => _dominantSpeaker;
            private set
            {
                var prev = _dominantSpeaker;
                _dominantSpeaker = value;

                if (prev != value)
                {
                    PreviousDominantSpeaker = prev;
                    DominantSpeakerChanged?.Invoke(value, prev);
                }
            }
        }

        public IStreamVideoCallParticipant PreviousDominantSpeaker { get; private set; }

        public IReadOnlyList<IStreamVideoCallParticipant> PinnedParticipants => _pinnedParticipants;
        //public IEnumerable<IStreamVideoCallParticipant> SortedParticipants => _sortedParticipants;

        #region State

        public bool Backstage { get; private set; }

        public IReadOnlyList<string> BlockedUserIds => _blockedUserIds;
        public IReadOnlyList<OwnCapability> OwnCapabilities => _ownCapabilities;
        public IEnumerable<CallMember> Members => _members.Values;
        public IEnumerable<IStreamVideoUser> BlockedUsers => _blockedUsers;

        public string Cid { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public IStreamVideoUser CreatedBy { get; private set; }

        public string CurrentSessionId { get; private set; }

        /// <summary>
        /// Custom data for this object
        /// </summary>
        //public System.Collections.Generic.Dictionary<string, object> Custom { get; set; } = new System.Collections.Generic.Dictionary<string, object>();
        //StreamTodo: ensure custom data is implemented by base type

        public CallEgress Egress { get; private set; }

        //StreamTodo: nullable? no date in DTO should resolve to null not 0000-00-00
        /// <summary>
        /// Date/time when the call ended
        /// </summary>
        public DateTimeOffset EndedAt { get; private set; }

        /// <summary>
        /// Call ID
        /// </summary>
        public string Id { get; private set; }

        public CallIngress Ingress { get; private set; }

        public bool Recording { get; private set; }

        //StreamTodo: perhaps this should be part of IStreamCall state
        public CallSession Session { get; private set; }

        public CallSettings Settings { get; private set; }

        //StreamTodo: nullable? no date in DTO should resolve to null not 0000-00-00
        /// <summary>
        /// Date/time when the call will start
        /// </summary>
        public DateTimeOffset StartsAt { get; private set; }

        public string Team { get; private set; }

        public bool Transcribing { get; private set; }

        /// <summary>
        /// The type of call
        /// </summary>
        public StreamCallType Type { get; private set; }

        /// <summary>
        /// Date/time of the last update
        /// </summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        // Below don't belong to CallResponse

        public bool Created { get; private set; }

        //StreamTodo: should credentials be internal? 
        public Credentials Credentials { get; private set; }
        public string Duration { get; private set; }
        public CallMember Membership { get; private set; }

        #endregion

        #region State from SFU

        #endregion

        public Task GoLiveAsync() => Client.GoLiveAsync(this);

        public Task StopLiveAsync() => Client.StopLiveAsync(this);

        public Task StartRecordingAsync() => Client.StartRecordingAsync(this);

        public Task StopRecordingAsync() => Client.StopRecordingAsync(this);

        public Task StartHLS() => LowLevelClient.InternalVideoClientApi.StartBroadcastingAsync(Type, Id);

        public Task StopHLS() => LowLevelClient.InternalVideoClientApi.StopBroadcastingAsync(Type, Id);

        public Task MuteAllUsersAsync(bool audio, bool video, bool screenShare)
            => Client.MuteAllUsersAsync(this, audio, video, screenShare);

        public Task MuteUsersAsync(IEnumerable<string> userIds, bool audio, bool video, bool screenShare)
        {
            var body = new MuteUsersRequestInternalDTO
            {
                Audio = audio,
                MuteAllUsers = false,
                Screenshare = screenShare,
                UserIds = userIds.ToList(),
                Video = video
            };
            return LowLevelClient.InternalVideoClientApi.MuteUsersAsync(Type, Id, body);
        }

        public Task MuteUsersAsync(IEnumerable<IStreamVideoUser> users, bool audio, bool video, bool screenShare)
            => MuteUsersAsync(users.Select(u => u.Id), audio, video, screenShare);

        public Task MuteUsersAsync(IEnumerable<IStreamVideoCallParticipant> participants, bool audio, bool video,
            bool screenShare)
            => MuteUsersAsync(participants.Select(u => u.UserId), audio, video, screenShare);

        public Task BlockUserAsync(string userId) => Client.BlockUserAsync(this, userId);

        public Task BlockUserAsync(IStreamVideoUser user) => Client.BlockUserAsync(this, user.Id);

        public Task BlockUserAsync(IStreamVideoCallParticipant participant)
            => Client.BlockUserAsync(this, participant.UserId);

        public Task UnblockUserAsync(string userId) => Client.UnblockUserAsync(this, userId);

        public Task UnblockUserAsync(IStreamVideoUser user) => Client.UnblockUserAsync(this, user.Id);

        public Task UnblockUserAsync(IStreamVideoCallParticipant participant)
            => Client.UnblockUserAsync(this, participant.UserId);

        /// <summary>
        /// Check if you currently have this permission
        /// </summary>
        /// <param name="permission">Permission to check</param>
        public bool HasPermissions(OwnCapability permission) => OwnCapabilities.Contains(permission);

        /// <summary>
        /// Request call host to grant you this permission
        /// </summary>
        /// <param name="permission">Requested permission</param>
        public Task RequestPermissionAsync(OwnCapability permission)
            => Client.RequestPermissionAsync(this, new List<string> { permission.ToString() });

        /// <summary>
        /// Request call host to grant you this permission
        /// </summary>
        /// <param name="permissions">Requested permission</param>
        public Task RequestPermissionsAsync(IEnumerable<OwnCapability> permissions)
            => Client.RequestPermissionAsync(this, permissions.Select(p => p.ToString()).ToList());

        //StreamTodo: p.ToString() will not work, we need to convert OwnCapability to string value defined in OwnCapabilityInternalEnum
        /// <summary>
        /// Grant permissions to a user in this call
        /// </summary>
        /// <param name="permissions">Permissions to grant</param>
        /// <param name="userId">User that will receive permissions</param>
        public Task GrantPermissionsAsync(IEnumerable<OwnCapability> permissions, string userId)
            => Client.UpdateUserPermissions(this, userId,
                grantPermissions: permissions.Select(p => p.ToString()).ToList(), revokePermissions: null);

        public Task GrantPermissionsAsync(IEnumerable<OwnCapability> permissions, IStreamVideoUser user)
            => GrantPermissionsAsync(permissions, user.Id);

        public Task GrantPermissionsAsync(IEnumerable<OwnCapability> permissions,
            IStreamVideoCallParticipant participant)
            => GrantPermissionsAsync(permissions, participant.UserId);

        /// <summary>
        /// Revoke permissions from a user in this call
        /// </summary>
        /// <param name="permissions">Permissions to revoke</param>
        /// <param name="userId">User that will have permissions revoked</param>
        public Task RevokePermissionsAsync(IEnumerable<OwnCapability> permissions, string userId)
            => Client.UpdateUserPermissions(this, userId,
                grantPermissions: null, revokePermissions: permissions.Select(p => p.ToString()).ToList());

        public Task RevokePermissionsAsync(IEnumerable<OwnCapability> permissions, IStreamVideoUser user)
            => RevokePermissionsAsync(permissions, user.Id);

        public Task RevokePermissionsAsync(IEnumerable<OwnCapability> permissions,
            IStreamVideoCallParticipant participant)
            => RevokePermissionsAsync(permissions, participant.UserId);

        public Task RemoveMembersAsync(IEnumerable<string> userIds)
            => LowLevelClient.InternalVideoClientApi.UpdateCallMembersAsync(Type, Id,
                new UpdateCallMembersRequestInternalDTO
                {
                    RemoveMembers = userIds.ToList(),
                });

        public Task RemoveMembersAsync(IEnumerable<IStreamVideoUser> users)
            => RemoveMembersAsync(users.Select(u => u.Id));

        public Task RemoveMembersAsync(IEnumerable<IStreamVideoCallParticipant> participants)
            => RemoveMembersAsync(participants.Select(u => u.UserId));

        public async Task<QueryMembersResult> QueryMembersAsync(IEnumerable<IFieldFilterRule> filters = null,
            CallMemberSort sort = null, int limit = 25, string prev = null, string next = null)
        {
            var request = new QueryMembersRequestInternalDTO
            {
                FilterConditions = filters?.Select(_ => _.GenerateFilterEntry()).ToDictionary(x => x.Key, x => x.Value),
                Id = Id,
                Limit = limit,
                Next = next,
                Prev = prev,
                Sort = sort?.ToSortParamRequestList(),
                Type = Type
            };

            var response = await LowLevelClient.InternalVideoClientApi.QueryMembersAsync(request);
            if (response == null || response.Members == null || response.Members.Count == 0)
            {
                return new QueryMembersResult();
            }

            var members = new List<CallMember>();
            foreach (var memberDto in response.Members)
            {
                var domain = new CallMember();
                var updateable = (IStateLoadableFrom<MemberResponseInternalDTO, CallMember>)domain;
                updateable.LoadFromDto(memberDto, Cache);
                members.Add(domain);
            }

            return new QueryMembersResult(members, response.Prev, response.Next);
        }

        public Task SendReactionAsync(string type)
            => LowLevelClient.InternalVideoClientApi.SendVideoReactionAsync(Type, Id,
                new SendReactionRequestInternalDTO
                {
                    Type = type
                });

        public Task SendReactionAsync(string type, string emojiCode, Dictionary<string, object> customData = null)
            => LowLevelClient.InternalVideoClientApi.SendVideoReactionAsync(Type, Id,
                new SendReactionRequestInternalDTO
                {
                    Custom = customData,
                    EmojiCode = emojiCode,
                    Type = type
                });

        public Task SendCustomEventAsync(Dictionary<string, object> eventData)
            => LowLevelClient.InternalVideoClientApi.SendEventAsync(Type, Id, new SendEventRequestInternalDTO
            {
                Custom = eventData
            });

        // public Task GetOrCreateAsync()
        // {
        //     return Task.CompletedTask;
        // }

        /**
   * Will start to watch for call related WebSocket events and initiate a call session with the server.
   *
   * @returns a promise which resolves once the call join-flow has finished.
   */
        // public Task JoinAsync()
        // {
        //     return Task.CompletedTask; //StreamTodo: implement
        // }
        public Task AcceptAsync() => LowLevelClient.InternalVideoClientApi.AcceptCallAsync(Type, Id);

        public Task RejectAsync() => LowLevelClient.InternalVideoClientApi.RejectCallAsync(Type, Id);

        public Task LeaveAsync()
        {
            //StreamTodo: review if we need any of this - on Android leave() makes -> remove "ActiveCall" in client.state, camera.disable(), microphone.disable()
            return Client.LeaveCallAsync(this);
        }

        public Task EndAsync() => Client.EndCallAsync(this);

        public void PinLocally(IStreamVideoCallParticipant participant)
        {
            _localPinsSessionIds.Remove(participant.SessionId);
            _localPinsSessionIds.AddFirst(participant.SessionId);

            UpdatePinnedParticipants();
            UpdateSortedParticipants();
        }

        public void UnpinLocally(IStreamVideoCallParticipant participant)
        {
            _localPinsSessionIds.Remove(participant.SessionId);

            UpdatePinnedParticipants();
            UpdateSortedParticipants();
        }

        public bool IsPinnedLocally(IStreamVideoCallParticipant participant)
            => _localPinsSessionIds.Contains(participant.SessionId);

        public bool IsPinnedRemotely(IStreamVideoCallParticipant participant)
            => _serverPinsSessionIds.Contains(participant.SessionId);

        public bool IsPinned(IStreamVideoCallParticipant participant)
            => IsPinnedLocally(participant) || IsPinnedRemotely(participant);

        //StreamTodo: add to docs
        public bool TryGetCapabilitiesByRole(string role, out IReadOnlyList<string> capabilities)
        {
            if (!_capabilitiesByRole.ContainsKey(role))
            {
                capabilities = null;
                return false;
            }

            capabilities = _capabilitiesByRole[role];
            return true;
        }

        void IUpdateableFrom<CallResponseInternalDTO, StreamCall>.UpdateFromDto(CallResponseInternalDTO dto,
            ICache cache)
        {
            Backstage = dto.Backstage;
            _blockedUserIds.TryReplaceValuesFromDto(dto.BlockedUserIds);
            Cid = dto.Cid;
            CreatedAt = dto.CreatedAt;
            CreatedBy = cache.TryCreateOrUpdate(dto.CreatedBy);
            CurrentSessionId = dto.CurrentSessionId;
            LoadCustomData(dto.Custom);
            Egress = cache.TryUpdateOrCreateFromDto(Egress, dto.Egress);
            EndedAt = dto.EndedAt;
            Id = dto.Id;
            Ingress = cache.TryUpdateOrCreateFromDto(Ingress, dto.Ingress);
            Recording = dto.Recording;
            Session = cache.TryUpdateOrCreateFromDto(Session, dto.Session);
            Settings = cache.TryUpdateOrCreateFromDto(Settings, dto.Settings);
            StartsAt = dto.StartsAt;
            Team = dto.Team;
            Transcribing = dto.Transcribing;
            Type = new StreamCallType(dto.Type);
            UpdatedAt = dto.UpdatedAt;
        }

        void IUpdateableFrom<GetCallResponseInternalDTO, StreamCall>.UpdateFromDto(GetCallResponseInternalDTO dto,
            ICache cache)
        {
            ((IUpdateableFrom<CallResponseInternalDTO, StreamCall>)this).UpdateFromDto(dto.Call, cache);

            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum,
                cache);
        }

        void IUpdateableFrom<GetOrCreateCallResponseInternalDTO, StreamCall>.UpdateFromDto(
            GetOrCreateCallResponseInternalDTO dto, ICache cache)
        {
            ((IUpdateableFrom<CallResponseInternalDTO, StreamCall>)this).UpdateFromDto(dto.Call, cache);

            Created = dto.Created;
            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum,
                cache);
        }

        void IUpdateableFrom<JoinCallResponseInternalDTO, StreamCall>.UpdateFromDto(JoinCallResponseInternalDTO dto,
            ICache cache)
        {
            ((IUpdateableFrom<CallResponseInternalDTO, StreamCall>)this).UpdateFromDto(dto.Call, cache);

            Created = dto.Created;
            Credentials = cache.TryUpdateOrCreateFromDto(Credentials, dto.Credentials);
            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum,
                cache);
        }

        void IUpdateableFrom<CallStateResponseFieldsInternalDTO, StreamCall>.UpdateFromDto(
            CallStateResponseFieldsInternalDTO dto, ICache cache)
        {
            ((IUpdateableFrom<CallResponseInternalDTO, StreamCall>)this).UpdateFromDto(dto.Call, cache);

            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum,
                cache);
        }

        //StreamTodo: handle state update from events, check Android CallState.kt handleEvent()

        internal StreamCall(string uniqueId, ICacheRepository<StreamCall> repository,
            IStatefulModelContext context)
            : base(uniqueId, repository, context)
        {
        }

        //StreamTodo: solve with a generic interface and best to be handled by cache layer
        internal void UpdateFromSfu(JoinResponse joinResponse)
        {
            ((IStateLoadableFrom<CallState, CallSession>)Session).LoadFromDto(joinResponse.CallState, Cache);
            UpdateServerPins(joinResponse.CallState.Pins);
        }

        internal void UpdateFromSfu(ParticipantJoined participantJoined, ICache cache)
        {
            var participant = Session.UpdateFromSfu(participantJoined, cache);
            ParticipantJoined?.Invoke(participant);
        }

        internal void UpdateFromSfu(ParticipantLeft participantLeft, ICache cache)
        {
            var participant = Session.UpdateFromSfu(participantLeft, cache);

            _localPinsSessionIds.RemoveAll(participant.sessionId);
            _serverPinsSessionIds.RemoveAll(pin => pin == participant.sessionId);
            UpdatePinnedParticipants();
            UpdateSortedParticipants();

            cache.CallParticipants.TryRemove(participant.sessionId);

            //StreamTodo: if we delete the participant from cache we should then pass SessionId and UserId
            ParticipantLeft?.Invoke(participant.sessionId, participant.userId);
        }

        internal void UpdateFromSfu(DominantSpeakerChanged dominantSpeakerChanged, ICache cache)
        {
            DominantSpeaker = Participants.FirstOrDefault(p => p.SessionId == dominantSpeakerChanged.SessionId);
        }

        internal void UpdateFromSfu(PinsChanged pinsChanged, ICache cache)
        {
            UpdateServerPins(pinsChanged.Pins);
            UpdatePinnedParticipants();
            UpdateSortedParticipants();
        }

        internal void NotifyTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
            => TrackAdded?.Invoke(participant, track);

        internal void UpdateCapabilitiesByRoleFromDto(CallUpdatedEventInternalDTO callUpdatedEvent)
            => UpdateCapabilitiesByRole(callUpdatedEvent.CapabilitiesByRole);

        internal void UpdateCapabilitiesByRoleFromDto(
            CallMemberUpdatedPermissionEventInternalDTO callMemberUpdatedPermissionEvent)
            => UpdateCapabilitiesByRole(callMemberUpdatedPermissionEvent.CapabilitiesByRole);

        internal void UpdateMembersFromDto(CallCreatedEventInternalDTO callCreatedEvent)
            => UpdateMembersFromDto(callCreatedEvent.Members);

        internal void UpdateMembersFromDto(CallMemberAddedEventInternalDTO callMemberAddedEvent)
            => UpdateMembersFromDto(callMemberAddedEvent.Members);

        internal void UpdateMembersFromDto(CallMemberUpdatedEventInternalDTO callMemberUpdatedEvent)
            => UpdateMembersFromDto(callMemberUpdatedEvent.Members);

        internal void UpdateMembersFromDto(
            CallMemberUpdatedPermissionEventInternalDTO callMemberUpdatedPermissionEvent)
            => UpdateMembersFromDto(callMemberUpdatedPermissionEvent.Members);

        internal void UpdateMembersFromDto(CallNotificationEventInternalDTO callNotificationEvent)
            => UpdateMembersFromDto(callNotificationEvent.Members);

        internal void UpdateMembersFromDto(CallRingEventInternalDTO callRingEvent)
            => UpdateMembersFromDto(callRingEvent.Members);

        internal void UpdateMembersFromDto(CallMemberRemovedEventInternalDTO callMemberRemovedEvent)
        {
            foreach (var removedMemberId in callMemberRemovedEvent.Members)
            {
                if (_members.ContainsKey(removedMemberId))
                {
                    _members.Remove(removedMemberId);
                }
            }
        }

        internal void UpdateOwnCapabilitiesFrom(
            UpdatedCallPermissionsEventInternalDTO updatedCallPermissionsEvent)
        {
            var ownCapabilities = updatedCallPermissionsEvent.OwnCapabilities;
            if (ownCapabilities == null || ownCapabilities.Count == 0)
            {
                return;
            }

            _ownCapabilities.Clear();
            foreach (var c in ownCapabilities)
            {
                var capability = c.ToPublicEnum();
                _ownCapabilities.Add(capability);
            }

            //StreamTodo: we should probably expose an event OwnCapabilitiesChanged
        }

        internal void InternalHandleCallRecordingStartedEvent(CallReactionEventInternalDTO callReactionEvent)
        {
            var reaction = new Reaction();
            Cache.TryUpdateOrCreateFromDto(reaction, callReactionEvent.Reaction);

            var participant
                = _client.RtcSession.ActiveCall.Participants.FirstOrDefault(p => p.UserId == reaction.User.Id);
            if (participant == null)
            {
                Logs.ErrorIfDebug(
                    $"Failed to find participant for reaction. UserId: {reaction.User.Id}, Participants: " +
                    string.Join(", ", _client.RtcSession.ActiveCall.Participants.Select(p => p.UserId)));
                return;
            }

            //StreamTodo: Android also keeps track of reactions per participant, each participant has reactions collections

            ReactionAdded?.Invoke(reaction, participant);
        }

        internal void InternalHandleCallRecordingStartedEvent(
            CallRecordingStartedEventInternalDTO callRecordingStartedEvent)
            => RecordingStarted?.Invoke();

        public void InternalHandleCallRecordingStoppedEvent(
            CallRecordingStoppedEventInternalDTO callRecordingStoppedEvent)
            => RecordingStopped?.Invoke();

        protected override string InternalUniqueId
        {
            get => Cid;
            set => Cid = value;
        }

        protected override StreamCall Self => this;

        #region State

        //StreamTodo: is this always in sync with _blockedUsers? 
        private readonly List<string> _blockedUserIds = new List<string>();

        // Below is not part of call response

        private readonly Dictionary<string, CallMember> _members = new Dictionary<string, CallMember>();
        private readonly List<OwnCapability> _ownCapabilities = new List<OwnCapability>();

        //StreamTodo: update this from BlockedUserEvent & UnblockedUserEvent + what about the initial state when we join? We only receive _blockedUserIds
        private readonly List<StreamVideoUser> _blockedUsers = new List<StreamVideoUser>();

        private readonly LinkedList<string> _localPinsSessionIds = new LinkedList<string>();
        private readonly List<string> _serverPinsSessionIds = new List<string>();

        private readonly List<IStreamVideoCallParticipant>
            _pinnedParticipants = new List<IStreamVideoCallParticipant>();

        private readonly Dictionary<string, List<string>> _capabilitiesByRole = new Dictionary<string, List<string>>();

        #endregion

        private readonly StreamVideoLowLevelClient _client;
        private readonly StreamCallType _type;

        private string _id;
        private IStreamVideoCallParticipant _dominantSpeaker;

        private void UpdateServerPins(IEnumerable<Pin> pins)
        {
            _serverPinsSessionIds.Clear();

            foreach (var pin in pins)
            {
                _serverPinsSessionIds.Add(pin.SessionId);
            }
        }

        private void UpdatePinnedParticipants()
        {
            _pinnedParticipants.Clear();

            //StreamTodo: use hashset pool to optimize
            foreach (var participant in Participants)
            {
                if (_serverPinsSessionIds.Contains(participant.SessionId))
                {
                    _pinnedParticipants.Add(participant);
                }
            }

            foreach (var participant in Participants)
            {
                if (_localPinsSessionIds.Contains(participant.SessionId))
                {
                    _pinnedParticipants.Add(participant);
                }
            }

            PinnedParticipantsUpdated?.Invoke();
        }

        private void UpdateSortedParticipants()
        {
            //SortedParticipantsUpdated?.Invoke();
        }

        private void UpdateMembersFromDto(IEnumerable<MemberResponseInternalDTO> membersDtos)
        {
            _members.TryUpdateOrCreateFromDto(membersDtos, keySelector: dtoItem => dtoItem.UserId, Cache);
        }

        private void UpdateCapabilitiesByRole(Dictionary<string, List<string>> capabilitiesByRole)
        {
            foreach (var role in _capabilitiesByRole.Keys)
            {
                if (!capabilitiesByRole.ContainsKey(role))
                {
                    _capabilitiesByRole.Remove(role);
                }
            }

            foreach (var roleCapabilities in capabilitiesByRole)
            {
                if (!_capabilitiesByRole.ContainsKey(roleCapabilities.Key))
                {
                    _capabilitiesByRole[roleCapabilities.Key] = new List<string>();
                }

                _capabilitiesByRole[roleCapabilities.Key].Clear();
                _capabilitiesByRole[roleCapabilities.Key].AddRange(roleCapabilities.Value);
            }

            //StreamTodo: according to description in CallUpdatedEventInternalDTO we should use also update the _ownCapabilities here based on the user role
        }
    }
}