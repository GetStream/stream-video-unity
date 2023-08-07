using System;
using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class CallSession : IStateLoadableFrom<CallSessionResponseInternalDTO, CallSession>
    {
        public IReadOnlyDictionary<string, DateTimeOffset> AcceptedBy => _acceptedBy;

        public DateTimeOffset EndedAt { get; private set; }

        public string Id { get; private set; }

        public IReadOnlyList<IStreamVideoCallParticipant> Participants => _participants;

        public IReadOnlyDictionary<string, int> ParticipantsCountByRole => _participantsCountByRole;

        public IReadOnlyDictionary<string, DateTimeOffset> RejectedBy => _rejectedBy;

        public DateTimeOffset StartedAt { get; private set; }

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
        }

        private readonly Dictionary<string, DateTimeOffset> _acceptedBy = new Dictionary<string, DateTimeOffset>();
        private readonly List<StreamVideoCallParticipant> _participants = new List<StreamVideoCallParticipant>();
        private readonly Dictionary<string, int> _participantsCountByRole = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTimeOffset> _rejectedBy = new Dictionary<string, DateTimeOffset>();
    }
}