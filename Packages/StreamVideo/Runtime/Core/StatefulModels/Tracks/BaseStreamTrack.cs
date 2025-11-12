using System;
using Unity.WebRTC;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public delegate void StreamTrackStateChangeHandler(bool isEnabled);
    
    public abstract class BaseStreamTrack : IStreamTrack
    {
        public event StreamTrackStateChangeHandler EnabledChanged;
        
        public bool IsEnabled => InternalTrack.Enabled;

        public void Dispose() => OnDisposing();

        internal BaseStreamTrack(MediaStreamTrack track)
        {
            InternalTrack = track ?? throw new ArgumentNullException(nameof(track));
        }
        
        internal virtual void Update()
        {
        }

        internal void SetEnabled(bool enabled)
        {
            InternalTrack.Enabled = enabled;
            EnabledChanged?.Invoke(enabled);
        }

        protected MediaStreamTrack InternalTrack { get; set; }

        protected virtual void OnDisposing()
        {
            
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