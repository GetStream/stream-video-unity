using System;
using System.Collections.Generic;
using StreamVideo.v1.Sfu.Events;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.Utils;
using SfuCallState = StreamVideo.v1.Sfu.Models.CallState;
using SfuParticipant = StreamVideo.v1.Sfu.Models.Participant;
using SfuParticipantCount = StreamVideo.v1.Sfu.Models.ParticipantCount;

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
            
            UpdateParticipantCountFromSessionInternal(dto.AnonymousParticipantCount, dto.ParticipantsCountByRole);
        }

        void IStateLoadableFrom<SfuCallState, CallSession>.LoadFromDto(SfuCallState dto, ICache cache)
        {
            if (dto.StartedAt != null)
            {
                StartedAt = dto.StartedAt.ToDateTimeOffset();
            }

            // dto.CallState.Participants may not contain all participants
            UpdateExtensions<StreamVideoCallParticipant, SfuParticipant>.TryAddUniqueTrackedObjects(_participants,
                dto.Participants, cache.CallParticipants);

            ((IStateLoadableFrom<SfuParticipantCount, ParticipantCount>)ParticipantCount).LoadFromDto(
                dto.ParticipantCount, cache);
        }

        internal IStreamVideoCallParticipant UpdateFromSfu(ParticipantJoined participantJoined, ICache cache)
        {
            var participant = cache.TryCreateOrUpdate(participantJoined.Participant);

            if (!_participants.Contains(participant))
            {
                _participants.Add(participant);
            }

            return participant;
        }

        internal void UpdateFromSfu(HealthCheckResponse healthCheckResponse, ICache cache)
        {
            ((IStateLoadableFrom<SfuParticipantCount, ParticipantCount>)ParticipantCount).LoadFromDto(
                healthCheckResponse.ParticipantCount, cache);
        }
        
        internal (string sessionId, string userId) UpdateFromSfu(ParticipantLeft participantLeft, ICache cache)
        {
            var participant = cache.TryCreateOrUpdate(participantLeft.Participant);
            _participants.Remove(participant);
            
            return (participantLeft.Participant.SessionId, participantLeft.Participant.UserId);
        }
        
        internal void UpdateFromCoordinator(
            InternalDTO.Events.CallSessionParticipantCountsUpdatedEventInternalDTO participantCountsUpdated,
            LowLevelClient.CallingState callingState)
        {
            _participantsCountByRole.TryReplaceValuesFromDto(participantCountsUpdated.ParticipantsCountByRole);
            UpdateParticipantCountFromCoordinator(participantCountsUpdated.AnonymousParticipantCount, 
                participantCountsUpdated.ParticipantsCountByRole, callingState);
        }
        
        internal void UpdateFromCoordinator(
            InternalDTO.Events.CallSessionParticipantJoinedEventInternalDTO participantJoined, ICache cache,
            LowLevelClient.CallingState callingState)
        {
            // Add or update participant in the list
            var participant = cache.TryCreateOrUpdate(participantJoined.Participant);
            
            if (!_participants.Contains(participant))
            {
                _participants.Add(participant);
            }
            
            // Update the role count - increment
            var role = participantJoined.Participant.Role;
            if (_participantsCountByRole.ContainsKey(role))
            {
                _participantsCountByRole[role]++;
            }
            else
            {
                _participantsCountByRole[role] = 1;
            }
            
            // Recalculate participant count
            var anonymousCount = 0; // We don't get this from the event, keep existing
            if (ParticipantCount != null)
            {
                anonymousCount = (int)ParticipantCount.Anonymous;
            }
            UpdateParticipantCountFromCoordinator(anonymousCount, _participantsCountByRole, callingState);
        }
        
        internal void UpdateFromCoordinator(
            InternalDTO.Events.CallSessionParticipantLeftEventInternalDTO participantLeft, ICache cache,
            LowLevelClient.CallingState callingState)
        {
            // Remove participant from the list
            var participant = cache.TryCreateOrUpdate(participantLeft.Participant);
            _participants.Remove(participant);
            
            // Update the role count - decrement
            var role = participantLeft.Participant.Role;
            if (_participantsCountByRole.ContainsKey(role))
            {
                _participantsCountByRole[role] = System.Math.Max(0, _participantsCountByRole[role] - 1);
                
                // Remove the role if count is 0
                if (_participantsCountByRole[role] == 0)
                {
                    _participantsCountByRole.Remove(role);
                }
            }
            
            // Recalculate participant count
            var anonymousCount = 0; // We don't get this from the event, keep existing
            if (ParticipantCount != null)
            {
                anonymousCount = (int)ParticipantCount.Anonymous;
            }
            UpdateParticipantCountFromCoordinator(anonymousCount, _participantsCountByRole, callingState);
        }
        
        /// <summary>
        /// Updates the ParticipantCount based on session data (used when NOT connected to SFU)
        /// </summary>
        private void UpdateParticipantCountFromCoordinator(int anonymousParticipantCount, 
            IReadOnlyDictionary<string, int> participantsCountByRole, LowLevelClient.CallingState callingState)
        {
            // When in JOINED state, we should use the participant count coming through
            // the SFU healthcheck event, as it's more accurate.
            if (callingState == LowLevelClient.CallingState.Joined)
            {
                return;
            }
            
            UpdateParticipantCountFromSessionInternal(anonymousParticipantCount, participantsCountByRole);
        }
        
        /// <summary>
        /// Updates the ParticipantCount based on session data
        /// </summary>
        private void UpdateParticipantCountFromSessionInternal(int anonymousParticipantCount, 
            IReadOnlyDictionary<string, int> participantsCountByRole)
        {
            // Calculate total from role counts
            var byRoleCount = 0;
            foreach (var count in participantsCountByRole.Values)
            {
                byRoleCount += count;
            }
            
            // Use the maximum of byRoleCount and actual participants list count
            var total = System.Math.Max(byRoleCount, _participants.Count);
            
            // Create a temporary DTO to update the ParticipantCount
            var dto = new SfuParticipantCount
            {
                Total = (uint)total,
                Anonymous = (uint)anonymousParticipantCount
            };
            
            ((IStateLoadableFrom<v1.Sfu.Models.ParticipantCount, ParticipantCount>)ParticipantCount)
                .LoadFromDto(dto, null);
        }

        private readonly Dictionary<string, DateTimeOffset> _acceptedBy = new Dictionary<string, DateTimeOffset>();
        private readonly List<StreamVideoCallParticipant> _participants = new List<StreamVideoCallParticipant>();
        private readonly Dictionary<string, int> _participantsCountByRole = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTimeOffset> _rejectedBy = new Dictionary<string, DateTimeOffset>();
    }
}