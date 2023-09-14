using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stream.Video.v1.Sfu.Events;
using Stream.Video.v1.Sfu.Models;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core
{
    /// <summary>
    /// Represents a call during which participants can share: audio, video, screen
    /// </summary>
    internal sealed class StreamCall : StreamStatefulModelBase<StreamCall>,
        IUpdateableFrom<CallResponseInternalDTO, StreamCall>,
        IUpdateableFrom<GetCallResponseInternalDTO, StreamCall>,
        IUpdateableFrom<GetOrCreateCallResponseInternalDTO, StreamCall>,
        IUpdateableFrom<JoinCallResponseInternalDTO, StreamCall>,
        IStreamCall
    {
        //StreamTodo: add sorted participants
        /**
     * Sorted participants gives you the list of participants sorted by
     * * anyone who is pinned
     * * dominant speaker
     * * if you are screensharing
     * * last speaking at
     * * all other video participants by when they joined
     * * audio only participants by when they joined
     *
     */
        public event ParticipantTrackChangedHandler TrackAdded;

        public event ParticipantJoinedHandler ParticipantJoined;
        public event ParticipantLeftHandler ParticipantLeft;

        public IReadOnlyList<IStreamVideoCallParticipant> Participants => Session.Participants;

        public bool IsLocalUserOwner
        {
            get
            {
                var localParticipant = Participants.FirstOrDefault(p => p.IsLocalParticipant);
                return CreatedBy.Id == localParticipant?.UserId;
            }
        }

        #region State

        public bool Backstage { get; private set; }

        public IReadOnlyList<string> BlockedUserIds => _blockedUserIds;
        public IReadOnlyList<OwnCapability> OwnCapabilities => _ownCapabilities;

        /// <summary>
        /// The unique identifier for a call (&lt;type&gt;:&lt;id&gt;)
        /// </summary>
        public string Cid { get; private set; }

        /// <summary>
        /// Date/time of creation
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// The user that created the call
        /// </summary>
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

        public Task GoLiveAsync() => Client.LeaveCallAsync(this);

        public Task StopLiveAsync() => Client.StopLiveAsync(this);
        
        public Task StartRecordingAsync() => Client.StartRecordingAsync(this);

        public Task StopRecordingAsync() => Client.StopRecordingAsync(this);

        public Task MuteAllUsersAsync(bool audio, bool video, bool screenShare)
            => Client.MuteAllUsersAsync(this, audio, video, screenShare);

        public Task BlockUserAsync(string userId) => Client.BlockUserAsync(this, userId);
        
        public Task BlockUserAsync(IStreamVideoUser user) => Client.BlockUserAsync(this, user.Id);
        
        public Task BlockUserAsync(IStreamVideoCallParticipant participant) => Client.BlockUserAsync(this, participant.UserId);

        public Task GetOrCreateAsync()
        {
            return Task.CompletedTask;
        }

        /**
   * Will start to watch for call related WebSocket events and initiate a call session with the server.
   *
   * @returns a promise which resolves once the call join-flow has finished.
   */
        public Task JoinAsync()
        {
            return Task.CompletedTask;
        }

        /**
   * Marks the incoming call as accepted.
   *
   * This method should be used only for "ringing" call flows.
   * {@link Call.join} invokes this method automatically for you when joining a call.
   * Unless you are implementing a custom "ringing" flow, you should not use this method.
   */
        public Task AcceptAsync()
        {
            return Task.CompletedTask;
        }

        /**
   * Marks the incoming call as rejected.
   *
   * This method should be used only for "ringing" call flows.
   * {@link Call.leave} invokes this method automatically for you when you leave or reject this call.
   * Unless you are implementing a custom "ringing" flow, you should not use this method.
   */
        public Task Reject()
        {
            return Task.CompletedTask;
        }

        public Task LeaveAsync()
        {
            //StreamTodo: review if we need any of this - on Android leave() makes -> remove "ActiveCall" in client.state, camera.disable(), microphone.disable()
            return Client.LeaveCallAsync(this);
        }

        public Task EndAsync() => Client.EndCallAsync(this);

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

            _blockedUsers.TryReplaceTrackedObjects(dto.BlockedUsers, cache.Users);
            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum,
                cache);
        }

        void IUpdateableFrom<GetOrCreateCallResponseInternalDTO, StreamCall>.UpdateFromDto(
            GetOrCreateCallResponseInternalDTO dto, ICache cache)
        {
            ((IUpdateableFrom<CallResponseInternalDTO, StreamCall>)this).UpdateFromDto(dto.Call, cache);

            _blockedUsers.TryReplaceTrackedObjects(dto.BlockedUsers, cache.Users);
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

            _blockedUsers.TryReplaceTrackedObjects(dto.BlockedUsers, cache.Users);
            Created = dto.Created;
            Credentials = cache.TryUpdateOrCreateFromDto(Credentials, dto.Credentials);
            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum,
                cache);
        }

        //StreamTodo: solve with a generic interface and best to be handled by cache layer
        internal void UpdateFromSfu(JoinResponse joinResponse)
        {
            ((IStateLoadableFrom<CallState, CallSession>)Session).LoadFromDto(joinResponse.CallState, Cache);
        }

        internal void UpdateFromSfu(ParticipantJoined participantJoined, ICache cache)
        {
            var participant = Session.UpdateFromSfu(participantJoined, cache);
            ParticipantJoined?.Invoke(participant);
        }

        internal void UpdateFromSfu(ParticipantLeft participantLeft, ICache cache)
        {
            var participant = Session.UpdateFromSfu(participantLeft, cache);

            //StreamTodo: if we delete the participant from cache we should then pass SessionId and UserId
            ParticipantLeft?.Invoke(participant.sessionId, participant.userId);
        }

        internal void NotifyTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
            => TrackAdded?.Invoke(participant, track);

        internal StreamCall(string uniqueId, ICacheRepository<StreamCall> repository,
            IStatefulModelContext context)
            : base(uniqueId, repository, context)
        {
        }

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
        private readonly List<StreamVideoUser> _blockedUsers = new List<StreamVideoUser>();

        #endregion

        private readonly StreamVideoLowLevelClient _client;
        private readonly StreamCallType _type;
        private string _id;
    }
}