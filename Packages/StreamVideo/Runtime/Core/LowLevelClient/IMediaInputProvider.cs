using System;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    internal interface IMediaInputProvider
    {
        event Action<AudioSource> AudioInputChanged;
        event Action<WebCamTexture> VideoInputChanged;
        event Action<Camera> VideoSceneInputChanged;
        
        AudioSource AudioInput { get; }
        WebCamTexture VideoInput { get; }
        Camera VideoSceneInput { get; set; }
    }
}