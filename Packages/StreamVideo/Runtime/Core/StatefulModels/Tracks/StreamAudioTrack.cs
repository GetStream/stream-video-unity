using System;
using Unity.WebRTC;
using UnityEngine;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamAudioTrack : BaseStreamTrack<AudioStreamTrack>
    {
        internal AudioSource TargetAudioSource;
        
        public StreamAudioTrack(AudioStreamTrack track) 
            : base(track)
        {

        }

        public void SetAudioSourceTarget(AudioSource audioSource)
        {
            if (audioSource == null)
            {
                throw new ArgumentNullException($"{nameof(audioSource)} cannot be null");
            }
            
            TargetAudioSource = audioSource;
            TargetAudioSource.SetTrack(Track);
            TargetAudioSource.loop = true;
            TargetAudioSource.Play();
        }
    }
}