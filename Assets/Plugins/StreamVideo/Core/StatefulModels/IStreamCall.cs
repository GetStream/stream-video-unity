using System.Collections.Generic;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models;
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

        event ParticipantJoinedHandler ParticipantJoined;
        event ParticipantLeftHandler ParticipantLeft;
    }
}