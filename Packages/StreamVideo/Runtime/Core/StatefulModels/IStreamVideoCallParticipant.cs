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
    public interface IStreamVideoCallParticipant : IStreamStatefulModel, IHasCustomData, IDisposable
    {
        /// <summary>
        /// A track was added for this participant. Tracks represents streams of video, and audio. You can get all tracks via <see cref="GetTracks"/>
        /// </summary>
        event ParticipantTrackChangedHandler TrackAdded;
        
        /// <summary>
        /// A track <see cref="IStreamTrack.IsEnabled"/> state changed for this participant.
        /// </summary>
        event ParticipantTrackChangedHandler TrackIsEnabledChanged;
        
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
        IStreamTrack VideoTrack { get; } //StreamTOdo: change to IVideoStreamTrack
        IStreamTrack AudioTrack { get; }
        IStreamTrack ScreenShareTrack { get; }
        DateTimeOffset JoinedAt { get; }
        float AudioLevel { get; }
        bool IsSpeaking { get; }
        ConnectionQuality ConnectionQuality { get; }

        /// <summary>
        /// Get all tracks associated with this participant. You can also use <see cref="TrackAdded"/> to get notified when a track is added
        /// </summary>
        IEnumerable<IStreamTrack> GetTracks();

        /// <summary>
        /// Set the desired video resolution for this participant. This should match the video resolution that you're displaying on the device.
        /// Using this function is critical to ensuring a smooth video experience, especially with multiple participants in a session. 
        /// By default, you receive other participants' videos in a high resolution, but as more participants join the session, this can quickly use up all of the available network bandwidth and lead to video stuttering.
        /// To optimize video performance, set the video resolution you display for every participant. Call this as often as the rendered resolution changes.
        ///
        /// StreamTodo: add link to the docs about best practices
        /// </summary>
        /// <param name="videoResolution">Video resolution you wish to receive for this participant. You can use a predefined size or pick a predefined one from <see cref="VideoResolution"/></param>
        void UpdateRequestedVideoResolution(VideoResolution videoResolution);

        /// <summary>
        /// Should video track of this participant be received.
        /// </summary>
        /// <param name="enabled">If enabled, the video stream will be requested from the server</param>
        void SetIncomingVideoEnabled(bool enabled);

        /// <summary>
        /// Should audio track of this participant be received
        /// </summary>
        /// <param name="enabled">If enabled, the audio stream will be requested from the server</param>
        void SetIncomingAudioEnabled(bool enabled);
    }
}