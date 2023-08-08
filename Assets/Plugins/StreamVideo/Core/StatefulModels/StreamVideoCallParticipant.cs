using System;
using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;
using Participant = Stream.Video.v1.Sfu.Models.Participant;

namespace StreamVideo.Core.StatefulModels
{
    internal sealed class StreamVideoCallParticipant : StreamStatefulModelBase<StreamVideoCallParticipant>,
        IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>, 
        IUpdateableFrom<Participant, StreamVideoCallParticipant>, 
        IStreamVideoCallParticipant
    {
        #region State

        public DateTimeOffset JoinedAt { get; private set; }

        public string Role { get; private set; }

        public IStreamVideoUser User { get; set; }

        public string UserSessionId { get; private set; }

        #endregion
        
        #region Sfu State
        
        public string UserId { get; private set; }
        
        public string SessionId { get; private set; }
        public IEnumerable<TrackType> PublishedTracks => _publishedTracks;
        public string TrackLookupPrefix  { get; private set; }
        public ConnectionQuality ConnectionQuality  { get; private set; }
        public bool IsSpeaking { get; private set; }
        public bool IsDominantSpeaker { get; private set; }
        public float AudioLevel { get; private set; }
        public string Name { get; private set; }
        public string Image  { get; private set; }
        public IEnumerable<string> Roles => _roles;

        #endregion

        public StreamVideoCallParticipant(string uniqueId, ICacheRepository<StreamVideoCallParticipant> repository,
            IStatefulModelContext context)
            : base(uniqueId, repository, context)
        {
        }

        //StreamTodo: perhaps distinguish to UpdateFromSfu interface
        void IUpdateableFrom<Participant, StreamVideoCallParticipant>.UpdateFromDto(Participant dto, ICache cache)
        {
            UserId = dto.UserId;
            SessionId = dto.SessionId;
            _publishedTracks.TryReplaceEnumsFromDtoCollection(dto.PublishedTracks, TrackTypeExt.ToPublicEnum, cache);
            TrackLookupPrefix = dto.TrackLookupPrefix;
            ConnectionQuality = dto.ConnectionQuality.ToPublicEnum();
            IsSpeaking = dto.IsSpeaking;
            IsDominantSpeaker = dto.IsDominantSpeaker;
            AudioLevel = dto.AudioLevel;
            Name = dto.Name;
            Image = dto.Image;
            _roles.TryReplaceValuesFromDto(dto.Roles);
        }

        void IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>.UpdateFromDto(
            CallParticipantResponseInternalDTO dto, ICache cache)
        {
            JoinedAt = dto.JoinedAt;
            Role = dto.Role;
            User = cache.TryCreateOrUpdate(dto.User);
            UserSessionId = dto.UserSessionId;
        }

        protected override string InternalUniqueId
        {
            get => UserSessionId;
            set => UserSessionId = value;
        }

        protected override StreamVideoCallParticipant Self => this;

        #region Sfu State

        private readonly List<TrackType> _publishedTracks = new List<TrackType>();
        private readonly List<string> _roles = new List<string>();

        #endregion
    }
}