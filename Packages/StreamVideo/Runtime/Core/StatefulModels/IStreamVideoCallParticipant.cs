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
        
        /// <summary>
        /// Is this participant "pinned" in the call meaning it will have precedence in <see cref="IStreamCall.SortedParticipants"/> list
        /// </summary>
        bool IsPinned { get; }
        
        /// <summary>
        /// Is this participant currently streaming a screen share track
        /// </summary>
        bool IsScreenSharing { get; }
        
        /// <summary>
        /// Is this participant currently streaming a video track
        /// </summary>
        bool IsVideoEnabled { get; }
        
        /// <summary>
        /// Is this participant currently streaming an audio track
        /// </summary>
        bool IsAudioEnabled { get; }
        
        /// <summary>
        /// Is this participant currently the most actively speaking participant.
        /// </summary>
        bool IsDominantSpeaker { get; }
        string UserId { get; }
        
        /// <summary>
        /// Session ID is a unique identifier for a <see cref="IStreamVideoUser"/> in a <see cref="IStreamCall"/>.
        /// A single user can join a call through multiple devices therefore a single call can have multiple participants with the same <see cref="UserId"/>.
        /// </summary>
        string SessionId { get; }
        string TrackLookupPrefix { get; }
        string Name { get; }
        
        /// <summary>
        /// Is this the participant from this device
        /// </summary>
        bool IsLocalParticipant { get; }
        IStreamVideoUser User { get; set; }
        IStreamTrack VideoTrack { get; }
        IStreamTrack AudioTrack { get; }
        IStreamTrack ScreenShareTrack { get; }
        DateTimeOffset JoinedAt { get; }
        float AudioLevel { get; }
        bool IsSpeaking { get; }
        ConnectionQuality ConnectionQuality { get; }

        IEnumerable<IStreamTrack> GetTracks();
    }
}