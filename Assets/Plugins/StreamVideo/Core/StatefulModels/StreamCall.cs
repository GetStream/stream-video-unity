using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
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
        #region State

        public bool Backstage { get; private set; }

        public IReadOnlyList<string> BlockedUserIds => _blockedUserIds;

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
        public StreamVideoUser CreatedBy { get; private set; }

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
        public string Type { get; private set; }

        /// <summary>
        /// Date/time of the last update
        /// </summary>
        public DateTimeOffset UpdatedAt { get; private set; }
        
        // Below don't belong to CallResponse
        
        public bool Created { get; private set; }
        public Credentials Credentials { get; private set; }
        public string Duration { get; private set; }
        public CallMember Membership { get; private set; }

        #endregion

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

        public Task Leave()
        {
            return Task.CompletedTask;
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
            Type = dto.Type;
            UpdatedAt = dto.UpdatedAt;
        }

        void IUpdateableFrom<GetCallResponseInternalDTO, StreamCall>.UpdateFromDto(GetCallResponseInternalDTO dto,
            ICache cache)
        {
            ((IUpdateableFrom<CallResponseInternalDTO, StreamCall>)this).UpdateFromDto(dto.Call, cache);

            _blockedUsers.TryReplaceTrackedObjects(dto.BlockedUsers, cache.Users);
            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum, cache);
        }

        void IUpdateableFrom<GetOrCreateCallResponseInternalDTO, StreamCall>.UpdateFromDto(
            GetOrCreateCallResponseInternalDTO dto, ICache cache)
        {
            ((IUpdateableFrom<CallResponseInternalDTO, StreamCall>)this).UpdateFromDto(dto.Call, cache);

            _blockedUsers.TryReplaceTrackedObjects(dto.BlockedUsers, cache.Users);
            Created = dto.Created;
            _members.TryUpdateOrCreateFromDto(dto.Members, keySelector: dtoItem => dtoItem.UserId, Cache);
            Membership = cache.TryUpdateOrCreateFromDto(Membership, dto.Membership);
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum, cache);
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
            _ownCapabilities.TryReplaceEnumsFromDtoCollection(dto.OwnCapabilities, OwnCapabilityExt.ToPublicEnum, cache);
        }


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