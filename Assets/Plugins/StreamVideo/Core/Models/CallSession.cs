using System;
using System.Collections.Generic;
using Stream.Video.v1.Sfu.Events;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.Utils;
using SfuCallState = Stream.Video.v1.Sfu.Models.CallState;
using SfuParticipant = Stream.Video.v1.Sfu.Models.Participant;
using SfuParticipantCount = Stream.Video.v1.Sfu.Models.ParticipantCount;

namespace StreamVideo.Core.Models
{
    public sealed class CallSession : IStateLoadableFrom<CallSessionResponseInternalDTO, CallSession>,
        IStateLoadableFrom<SfuCallState, CallSession>
    {
        public IReadOnlyDictionary<string, DateTimeOffset> AcceptedBy => _acceptedBy;

        public DateTimeOffset EndedAt { get; private set; }

        public string Id { get; private set; }

        /// <summary>
        /// In large calls, the list could be truncated in which
        /// case, the list of participants contains fewer participants
        /// than the counts returned in participant_count. Anonymous
        /// participants are **NOT** included in the list.
        /// </summary>
        public IReadOnlyList<IStreamVideoCallParticipant> Participants => _participants;

        //StreamTodo: what does string key represent?
        public IReadOnlyDictionary<string, int> ParticipantsCountByRole => _participantsCountByRole;

        public IReadOnlyDictionary<string, DateTimeOffset> RejectedBy => _rejectedBy;

        public DateTimeOffset StartedAt { get; private set; }

        public DateTimeOffset LiveEndedAt { get; private set; }

        public DateTimeOffset LiveStartedAt { get; private set; }

        #region Sfu State

        public ParticipantCount ParticipantCount { get; private set; } = new ParticipantCount();

        #endregion

        void IStateLoadableFrom<CallSessionResponseInternalDTO, CallSession>.LoadFromDto(
            CallSessionResponseInternalDTO dto, ICache cache)
        {
            _acceptedBy.TryReplaceValuesFromDto(dto.AcceptedBy);
            EndedAt = dto.EndedAt;
            Id = dto.Id;
            _participants.TryReplaceTrackedObjects(dto.Participants, cache.CallParticipants);
            _participantsCountByRole.TryReplaceValuesFromDto(dto.ParticipantsCountByRole);
            _rejectedBy.TryReplaceValuesFromDto(dto.RejectedBy);
            StartedAt = dto.StartedAt;
            LiveEndedAt = dto.LiveEndedAt;
            LiveStartedAt = dto.LiveStartedAt;
        }

        void IStateLoadableFrom<SfuCallState, CallSession>.LoadFromDto(SfuCallState dto, ICache cache)
        {
            //StreamTOdo: does StartedAt always have value?
            StartedAt = dto.StartedAt.ToDateTimeOffset();

            // dto.CallState.Participants may not contain all of the participants
            UpdateExtensions<StreamVideoCallParticipant, SfuParticipant>.TryAddUniqueTrackedObjects(_participants,
                dto.Participants, cache.CallParticipants);

            ((IStateLoadableFrom<SfuParticipantCount, ParticipantCount>)ParticipantCount).LoadFromDto(
                dto.ParticipantCount, cache);
        }

        internal void UpdateFromSfu(ParticipantJoined participantJoined, ICache cache)
        {
            var participant = cache.TryCreateOrUpdate(participantJoined.Participant);

            if (_participants.Contains(participant))
            {
                _participants.Add(participant);
            }
        }
        
        internal void UpdateFromSfu(ParticipantLeft participantLeft, ICache cache)
        {
            var participant = cache.TryCreateOrUpdate(participantLeft.Participant);
            _participants.Remove(participant);
            
            //StreamTodo: we should either remove the participant from cache or somehow mark to be removed. Otherwise cache will grow while the app is running
        }

        private readonly Dictionary<string, DateTimeOffset> _acceptedBy = new Dictionary<string, DateTimeOffset>();
        private readonly List<StreamVideoCallParticipant> _participants = new List<StreamVideoCallParticipant>();
        private readonly Dictionary<string, int> _participantsCountByRole = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTimeOffset> _rejectedBy = new Dictionary<string, DateTimeOffset>();
    }
}