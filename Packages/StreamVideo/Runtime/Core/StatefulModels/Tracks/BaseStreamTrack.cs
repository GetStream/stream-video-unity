using System;
using Unity.WebRTC;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public delegate void StreamTrackStateChangeHandler(bool isEnabled);
    
    /// <summary>
    /// Used for tracks of remote participants
    /// </summary>
    public abstract class BaseStreamTrack : IStreamTrack
    {
        public event StreamTrackStateChangeHandler EnabledChanged;

        /// <summary>
        /// Effective enabled state: true only when the publisher has the track enabled
        /// AND the Stream Server (SFU) has not paused it.
        /// </summary>
        public bool IsEnabled => _publisherEnabled && !_serverPaused;

        /// <summary>
        /// Whether the Stream Server (SFU) has paused this inbound track (e.g. due to insufficient bandwidth).
        /// </summary>
        public bool IsPausedByServer => _serverPaused;

        public void Dispose() => OnDisposing();

        internal BaseStreamTrack(MediaStreamTrack track)
        {
            InternalTrack = track ?? throw new ArgumentNullException(nameof(track));
            _publisherEnabled = track.Enabled;
        }
        
        internal virtual void Update()
        {
        }

        internal void SetPublisherEnabled(bool enabled)
        {
            if (_publisherEnabled == enabled)
            {
                return;
            }

            var wasEnabled = IsEnabled;
            _publisherEnabled = enabled;
            NotifyIfEffectiveStateChanged(wasEnabled);
            
            //StreamTodo: investigate this. In theory we should disable track whenever the remote user disabled it.
            //But there's and edge case where:
            // 1. host enables video device right after JoinCall
            // 2. host is in a call -> disables the camera (this disabled the track on the watcher user) -> leaves the call
            // 3. host enters the call -> the camera is enabled right after JoinCall -> the watcher sees only black frames because track is disabled on his side
            // Investigate missing implementation Context::StopMediaStreamTrack(webrtc::MediaStreamTrackInterface* track)
            
            // InternalTrack.Enabled = enabled;
        }

        internal void SetServerPaused(bool paused)
        {
            if (_serverPaused == paused)
            {
                return;
            }

            var wasEnabled = IsEnabled;
            _serverPaused = paused;
            NotifyIfEffectiveStateChanged(wasEnabled);
        }

        protected MediaStreamTrack InternalTrack { get; set; }

        protected virtual void OnDisposing()
        {
            
        }

        private bool _publisherEnabled;
        private bool _serverPaused;

        private void NotifyIfEffectiveStateChanged(bool wasEnabled)
        {
            if (IsEnabled != wasEnabled)
            {
                EnabledChanged?.Invoke(IsEnabled);
            }
        }
    }

    public abstract class BaseStreamTrack<TTrack> : BaseStreamTrack
        where TTrack : MediaStreamTrack
    {
        protected TTrack Track { get; }

        protected BaseStreamTrack(MediaStreamTrack track) 
            : base(track)
        {
            Track = (TTrack)track;
        }
    }
}