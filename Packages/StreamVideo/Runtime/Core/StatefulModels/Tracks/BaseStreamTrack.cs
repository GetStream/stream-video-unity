using System;
using Unity.WebRTC;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public abstract class BaseStreamTrack : IStreamTrack
    {
        //StreamTodo: should we check ReadyState as well or is Enabled flag covering this?
        public bool Enabled => InternalTrack.Enabled;

        internal BaseStreamTrack(MediaStreamTrack track)
        {
            InternalTrack = track ?? throw new ArgumentNullException(nameof(track));
        }
        
        internal virtual void Update()
        {
            
        }

        internal void SetEnabled(bool enabled) => InternalTrack.Enabled = enabled;

        protected MediaStreamTrack InternalTrack { get; set; }
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