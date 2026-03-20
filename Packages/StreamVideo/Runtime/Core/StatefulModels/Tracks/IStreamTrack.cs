using System;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public interface IStreamTrack : IDisposable
    {
        /// <summary>
        /// Event triggered when the enabled state changes
        /// </summary>
        event StreamTrackStateChangeHandler EnabledChanged;

        /// <summary>
        /// Is this track active. This is false when either the publisher has disabled
        /// the track or the Stream Server (SFU) has paused it (e.g. due to insufficient bandwidth).
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Whether the Stream Server (SFU) has paused this inbound track due to bandwidth constraints.
        /// Use this to distinguish "publisher turned off the camera" from
        /// "video paused by the server due to poor network conditions".
        /// </summary>
        bool IsPausedByServer { get; }
    }
}