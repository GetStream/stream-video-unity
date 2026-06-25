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

        /// <summary>
        /// Fired when a new participant is added to the <see cref="Participants"/> list.
        /// </summary>
        internal event Action<IStreamVideoCallParticipant> ParticipantAdded;

        /// <summary>
        /// Fired when a participant is removed from the <see cref="Participants"/> list.
        /// </summary>
        internal event Action<string, string> ParticipantRemoved;

        void IStateLoadableFrom<CallSessionResponseInternalDTO, CallSession>.LoadFromDto(
            CallSessionResponseInternalDTO dto, ICache cache)
        {
            _acceptedBy.TryReplaceValuesFromDto(dto.AcceptedBy);
            EndedAt = dto.EndedAt;
            Id = dto.Id;

            // CallSessionResponseInternalDTO usually (or always?) contains no participants. Participants are updated from the SFU join response
            // But SFU response can arrive before API response, so we can't override participants here because this clears the list

            //StreamTODO: temp remove this. This seems to be only messing up the participants list. We're testing updating the participants only based on SFU data.
            // But we need to check how this will work with GetCall where there's not SFU connection

            // foreach (var dtoParticipant in dto.Participants)
            // {
            //     var participant = cache.TryCreateOrUpdate(dtoParticipant);
            //     if (!_participants.Contains(participant))
            //     {
            //         _participants.Add(participant);
            //     }
            // }

            // StreamTODO: figure out how to best handle this. Should we update it from coordinator or only the SFU
            //_participantsCountByRole.TryReplaceValuesFromDto(dto.ParticipantsCountByRole);
            _rejectedBy.TryReplaceValuesFromDto(dto.RejectedBy);
            StartedAt = dto.StartedAt;
            LiveEndedAt = dto.LiveEndedAt;
            LiveStartedAt = dto.LiveStartedAt;

            UpdateParticipantCountFromSessionInternal(dto.AnonymousParticipantCount, dto.ParticipantsCountByRole);
        }

        void IStateLoadableFrom<SfuCallState, CallSession>.LoadFromDto(SfuCallState dto, ICache cache)
        {
#if STREAM_DEBUG_ENABLED
            if (dto == null || cache == null || dto.Participants == null || dto.ParticipantCount == null)
            {
                throw new ArgumentNullException(
                    nameof(dto),
                    $"{nameof(CallSession)}.LoadFromDto(SfuCallState) precondition failed. " +
                    $"dto: {dto != null}, " +
                    $"cache: {cache != null}, " +
                    $"dto.Participants: {dto?.Participants != null} (count: {dto?.Participants?.Count ?? 0}), " +
                    $"dto.ParticipantCount: {dto?.ParticipantCount != null}, " +
                    $"dto.StartedAt: {dto?.StartedAt != null}, " +
                    $"dto.Pins: {dto?.Pins != null}");
            }
#endif

            if (dto.StartedAt != null)
            {
                StartedAt = dto.StartedAt.ToDateTimeOffset();
            }

            using (new HashSetPoolScope<string>(out var tempPrevSessionIds))
            using (new ListPoolScope<(string sessionId, string userId)>(out var tempRemovedParticipants))
            using (new HashSetPoolScope<string>(out var tempNewSessionIds))
            {
                foreach (var p in _participants)
                {
                    tempPrevSessionIds.Add(p.SessionId);
                }

                // Treat SFU as the most updated source of truth for participants
                _participants.Clear();

                // dto.CallState.Participants may not contain all participants
                foreach (var dtoParticipant in dto.Participants)
                {
                    var participant = cache.TryCreateOrUpdate(dtoParticipant);
                    if (tempNewSessionIds.Add(participant.SessionId))
                    {
                        _participants.Add(participant);
                    }
                }

                foreach (var prevId in tempPrevSessionIds)
                {
                    if (!tempNewSessionIds.Contains(prevId))
                    {
                        // Look up userId from cache before the participant is cleaned up
                        var userId = cache.CallParticipants.TryGet(prevId, out var removedP)
                            ? removedP.UserId
                            : string.Empty;
                        tempRemovedParticipants.Add((prevId, userId));
                        cache.CallParticipants.TryRemove(prevId);
                    }
                }

                foreach (var (sessionId, userId) in tempRemovedParticipants)
                {
                    ParticipantRemoved?.Invoke(sessionId, userId);
                }

                foreach (var p in _participants)
                {
                    if (!tempPrevSessionIds.Contains(p.SessionId))
                    {
                        ParticipantAdded?.Invoke(p);
                    }
                }
            }

            ((IStateLoadableFrom<SfuParticipantCount, ParticipantCount>)ParticipantCount).LoadFromDto(
                dto.ParticipantCount, cache);
        }

        internal void UpdateFromSfu(ParticipantJoined participantJoined, ICache cache)
        {
            var participant = cache.TryCreateOrUpdate(participantJoined.Participant);

            if (!_participants.Contains(participant))
            {
                _participants.Add(participant);
                ParticipantAdded?.Invoke(participant);
            }
        }

        internal void UpdateFromSfu(HealthCheckResponse healthCheckResponse, ICache cache)
        {
            ((IStateLoadableFrom<SfuParticipantCount, ParticipantCount>)ParticipantCount).LoadFromDto(
                healthCheckResponse.ParticipantCount, cache);
        }

        internal void UpdateFromSfu(ParticipantLeft participantLeft, ICache cache)
        {
            var participant = cache.TryCreateOrUpdate(participantLeft.Participant);

            if (!participant.IsLocalParticipant && _participants.Remove(participant))
            {
                ParticipantRemoved?.Invoke(participantLeft.Participant.SessionId,
                    participantLeft.Participant.UserId);
                cache.CallParticipants.TryRemove(participantLeft.Participant.SessionId);
            }
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
            var participant = cache.TryCreateOrUpdate(participantJoined.Participant);

            var isNew = !_participants.Contains(participant);
            if (isNew)
            {
                _participants.Add(participant);
            }

            var role = participantJoined.Participant.Role;
            if (_participantsCountByRole.ContainsKey(role))
            {
                _participantsCountByRole[role]++;
            }
            else
            {
                _participantsCountByRole[role] = 1;
            }

            var anonymousCount = ParticipantCount != null ? (int)ParticipantCount.Anonymous : 0;
            UpdateParticipantCountFromCoordinator(anonymousCount, _participantsCountByRole, callingState);

            if (isNew)
            {
                ParticipantAdded?.Invoke(participant);
            }
        }

        //StreamTODO: double-check this logic
        internal void UpdateFromCoordinator(
            InternalDTO.Events.CallSessionParticipantLeftEventInternalDTO participantLeft, ICache cache,
            LowLevelClient.CallingState callingState)
        {
            var participant = cache.TryCreateOrUpdate(participantLeft.Participant);
            var wasRemoved = _participants.Remove(participant);

            var role = participantLeft.Participant.Role;
            if (_participantsCountByRole.ContainsKey(role))
            {
                _participantsCountByRole[role] = Math.Max(0, _participantsCountByRole[role] - 1);

                if (_participantsCountByRole[role] == 0)
                {
                    _participantsCountByRole.Remove(role);
                }
            }

            var anonymousCount = ParticipantCount != null ? (int)ParticipantCount.Anonymous : 0;
            UpdateParticipantCountFromCoordinator(anonymousCount, _participantsCountByRole, callingState);

            if (wasRemoved)
            {
                ParticipantRemoved?.Invoke(participant.SessionId, participant.UserId);
            }
        }

        private readonly Dictionary<string, DateTimeOffset> _acceptedBy = new Dictionary<string, DateTimeOffset>();
        private readonly List<StreamVideoCallParticipant> _participants = new List<StreamVideoCallParticipant>();
        private readonly Dictionary<string, int> _participantsCountByRole = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTimeOffset> _rejectedBy = new Dictionary<string, DateTimeOffset>();

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
            var byRoleCount = 0;
            foreach (var count in participantsCountByRole.Values)
            {
                byRoleCount += count;
            }

            var total = Math.Max(byRoleCount, _participants.Count);

            var dto = new SfuParticipantCount
            {
                Total = (uint)total,
                Anonymous = (uint)anonymousParticipantCount
            };

            ((IStateLoadableFrom<SfuParticipantCount, ParticipantCount>)ParticipantCount)
                .LoadFromDto(dto, null);
        }

        internal void UpdateFromSfu(AudioLevelChanged audioLevelChanged)
        {
            foreach (var entry in audioLevelChanged.AudioLevels)
            {
                for (int i = 0; i < _participants.Count; i++)
                {
                    if (_participants[i].SessionId == entry.SessionId)
                    {
                        _participants[i].UpdateFromSfu(entry);
                    }
                }
            }
        }

        internal void UpdateFromSfu(ConnectionQualityChanged connectionQualityChanged)
        {
            foreach (var update in connectionQualityChanged.ConnectionQualityUpdates)
            {
                for (int i = 0; i < _participants.Count; i++)
                {
                    if (_participants[i].SessionId == update.SessionId)
                    {
                        _participants[i].UpdateFromSfu(update);
                    }
                }
            }
        }
    }
}