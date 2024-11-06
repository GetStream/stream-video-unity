using System;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    internal interface IMediaInputProvider
    {
        event Action<AudioSource> AudioInputChanged;
        event Action<WebCamTexture> VideoInputWebCamTextureChanged;
        event Action<RenderTexture> VideoInputRenderTextureChanged;
        event Action<Texture2D> VideoInputTexture2DChanged;
        event Action<Camera> VideoSceneInputChanged;
        
        AudioSource AudioInput { get; }
        WebCamTexture VideoWebCamTextureInput { get; }
        RenderTexture VideoRenderTextureInput { get; set; }
        Texture2D VideoTexture2DInput { get; set; }
        Camera VideoSceneInput { get; set; }
    }
}