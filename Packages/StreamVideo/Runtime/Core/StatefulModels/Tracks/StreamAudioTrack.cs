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
        
        /// <summary>
        ///     Mutes this audio track locally without affecting other users.
        /// </summary>
        /// <remarks>
        ///     `MuteLocally` mutes this audio track on the local device only. 
        ///     Other users in the call will not be affected. This is useful for temporarily
        ///     stopping playback of incoming audio without disconnecting the track.
        ///     This method is only available on Android platform.
        /// </remarks>
        /// <example>
        ///     <code lang="cs"><![CDATA[
        ///         audioStreamTrack.MuteLocally();
        ///     ]]></code>
        /// </example>
        /// <seealso cref="UnmuteLocally"/>
        /// <seealso cref="IsLocallyMuted"/>
        public void MuteLocally()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Track.MuteLocally();
#else
            throw new NotSupportedException($"The {nameof(MuteLocally)} method is currently only supported on Android platform.");
#endif
        }

        /// <summary>
        ///     Unmutes this audio track locally.
        /// </summary>
        /// <remarks>
        ///     `UnmuteLocally` unmutes a previously muted audio track on the local device.
        ///     This method is only available on Android platform.
        /// </remarks>
        /// <example>
        ///     <code lang="cs"><![CDATA[
        ///         audioStreamTrack.UnmuteLocally();
        ///     ]]></code>
        /// </example>
        /// <seealso cref="MuteLocally"/>
        /// <seealso cref="IsLocallyMuted"/>
        public void UnmuteLocally()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Track.UnmuteLocally();
#else
            throw new NotSupportedException($"The {nameof(UnmuteLocally)} method is currently only supported on Android platform.");
#endif
        }

        /// <summary>
        ///     Checks if this audio track is locally muted.
        /// </summary>
        /// <remarks>
        ///     `IsLocallyMuted` returns true if the track is currently muted on the local device.
        ///     This method is only available on Android platform.
        /// </remarks>
        /// <returns>True if the track is locally muted, false otherwise.</returns>
        /// <example>
        ///     <code lang="cs"><![CDATA[
        ///         if (audioStreamTrack.IsLocallyMuted())
        ///         {
        ///             Debug.Log("Track is muted locally");
        ///         }
        ///     ]]></code>
        /// </example>
        /// <seealso cref="MuteLocally"/>
        /// <seealso cref="UnmuteLocally"/>
        public bool IsLocallyMuted()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Track.IsLocallyMuted();
#else
            throw new NotSupportedException($"The {nameof(IsLocallyMuted)} method is currently only supported on Android platform.");
#endif
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