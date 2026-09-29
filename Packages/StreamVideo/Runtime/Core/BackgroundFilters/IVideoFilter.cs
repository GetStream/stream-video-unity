using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Internal filter hook. Must write into <paramref name="destination"/> on the main thread
    /// without <c>ReadPixels</c> of the publish texture.
    /// </summary>
    internal interface IVideoFilter
    {
        void Apply(Texture source, RenderTexture destination);
    }
}
