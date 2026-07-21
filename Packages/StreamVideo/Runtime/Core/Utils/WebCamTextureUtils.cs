using UnityEngine;

namespace StreamVideo.Core.Utils
{
    internal static class WebCamTextureUtils
    {
        /// <summary>
        /// Unity reports 16×16 on <see cref="WebCamTexture"/> until the camera stream is initialized.
        /// </summary>
        public static bool HasInitializedResolution(WebCamTexture texture)
        {
            if (texture == null || !texture.isPlaying)
            {
                return false;
            }

            return texture.width > 16 && texture.height > 16;
        }
    }
}
