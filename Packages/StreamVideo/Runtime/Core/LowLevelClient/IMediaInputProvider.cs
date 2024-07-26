using System;
using StreamVideo.Core.StatefulModels;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    internal interface IMediaInputProvider
    {
        event Action<AudioSource> AudioInputChanged;
        event Action<WebCamTexture> VideoInputChanged;
        event Action<Camera> VideoSceneInputChanged;
        
        event Action<(CustomTrackHandle handle, RenderTexture source, uint frameRate)> VideoSourceAdded;
        event Action<CustomTrackHandle> VideoSourceRemoved;
        
        AudioSource AudioInput { get; }
        WebCamTexture VideoInput { get; }
        Camera VideoSceneInput { get; set; }
    }
}