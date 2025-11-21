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

        public bool IsEnabled
        {
            get => _isEnabled;
            private set
            {
                if(value == _isEnabled)
                {
                    return;
                }
                
                _isEnabled = value;
                EnabledChanged?.Invoke(_isEnabled);
            }
        }

        public void Dispose() => OnDisposing();

        internal BaseStreamTrack(MediaStreamTrack track)
        {
            InternalTrack = track ?? throw new ArgumentNullException(nameof(track));
            _isEnabled = track.Enabled;
        }
        
        internal virtual void Update()
        {
        }

        internal void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            
            //StreamTodo: investigate this. In theory we should disable track whenever the remote user disabled it.
            //But there's and edge case where:
            // 1. host enables video device right after JoinCall
            // 2. host is in a call -> disables the camera (this disabled the track on the watcher user) -> leaves the call
            // 3. host enters the call -> the camera is enabled right after JoinCall -> the watcher sees only black frames because track is disabled on his side
            // Investigate missing implementation Context::StopMediaStreamTrack(webrtc::MediaStreamTrackInterface* track)
            
            // InternalTrack.Enabled = enabled;
        }

        protected MediaStreamTrack InternalTrack { get; set; }

        protected virtual void OnDisposing()
        {
            
        }
        
        private bool _isEnabled;
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