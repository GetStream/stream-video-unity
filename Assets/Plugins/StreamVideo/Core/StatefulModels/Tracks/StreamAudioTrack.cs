using Unity.WebRTC;
using UnityEngine;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamAudioTrack : BaseStreamTrack<AudioStreamTrack>
    {
        public void SetAudioSourceTarget(AudioSource audioSource)
        {
            audioSource.SetTrack(Track);
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}