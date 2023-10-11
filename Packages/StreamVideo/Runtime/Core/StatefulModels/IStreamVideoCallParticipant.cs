using System;
using System.Collections.Generic;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models.Sfu;
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
        IStreamVideoUser User { get; set; }
        IStreamTrack VideoTrack { get; }
        IStreamTrack AudioTrack { get; }
        IStreamTrack ScreenShareTrack { get; }
        DateTimeOffset JoinedAt { get; }
        float AudioLevel { get; }
        bool IsSpeaking { get; }
        ConnectionQuality ConnectionQuality { get; }
        bool IsDominantSpeaker { get; }

        IEnumerable<IStreamTrack> GetTracks();
    }
}