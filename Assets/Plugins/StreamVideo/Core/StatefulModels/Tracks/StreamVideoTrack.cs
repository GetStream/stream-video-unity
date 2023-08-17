using Unity.WebRTC;
using UnityEngine;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamVideoTrack : BaseStreamTrack<VideoStreamTrack>
    {
        //StreamTodo: can we remove Unity dependency? 
        public Texture Texture { get; private set; }
    }
}