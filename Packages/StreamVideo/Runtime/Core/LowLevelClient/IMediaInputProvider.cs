using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    internal interface IMediaInputProvider
    {
        AudioSource AudioInput { get; }
        WebCamTexture VideoInput { get; }
        Camera VideoSceneInput { get; set; }
    }
}