using System.Collections.Generic;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.State;
using StreamVideo.Core.StatefulModels.Tracks;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// A <see cref="IStreamVideoUser"/> that is actively connected to a <see cref="IStreamCall"/>
    /// </summary>
    public interface IStreamVideoCallParticipant : IStreamStatefulModel
    {
        event ParticipantTrackChangedHandler TrackAdded;

        string UserId { get; }
        string SessionId { get; }
        string TrackLookupPrefix { get; }
        string Name { get; }
        bool IsLocalParticipant { get; }

        IEnumerable<IStreamTrack> GetTracks();
    }
}