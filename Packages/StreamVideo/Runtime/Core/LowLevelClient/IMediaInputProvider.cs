using System;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    internal interface IMediaInputProvider
    {
        event Action<AudioSource> AudioInputChanged;
        event Action<WebCamTexture> VideoInputChanged;
        event Action<Camera> VideoSceneInputChanged;

        event Action<bool> PublisherAudioTrackIsEnabledChanged;
        event Action<bool> PublisherVideoTrackIsEnabledChanged;
        
        AudioSource AudioInput { get; }
        WebCamTexture VideoInput { get; }
        Camera VideoSceneInput { get; set; }
        
        /// <summary>
        /// Is Publisher audio track enabled. If the call is not active yet, the value is cached and will be applied when the call starts.
        /// </summary>
        bool PublisherAudioTrackIsEnabled { get; }
        
        /// <summary>
        /// Is Publisher video track enabled. If the call is not active yet, the value is cached and will be applied when the call starts.
        /// </summary>
        bool PublisherVideoTrackIsEnabled { get; }
    }
}