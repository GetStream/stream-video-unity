using System;
using Unity.WebRTC;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public abstract class BaseStreamTrack<TTrack> : IStreamTrack
        where TTrack : MediaStreamTrack
    {
        //StreamTodo: should we check ReadyState as well or is Enabled flag covering this?
        public bool IsActive => Track?.Enabled ?? false;
        
        public TTrack Track { get; private set; }

        internal void SetTrack(TTrack track)
        {
            Track = track ?? throw new ArgumentNullException(nameof(track));
        }
    }
}