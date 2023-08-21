using Unity.WebRTC;
using UnityEngine;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamAudioTrack : BaseStreamTrack<AudioStreamTrack>
    {
        public StreamAudioTrack(AudioStreamTrack track) 
            : base(track)
        {

        }

        public void SetAudioSourceTarget(AudioSource audioSource)
        {
            audioSource.SetTrack(Track);
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}