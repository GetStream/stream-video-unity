#if STREAM_TESTS_ENABLED
using UnityEngine;

namespace StreamVideo.Tests.Shared.DisposableAssets
{
    public class WebCamTextureFactory : DisposableAssetFactory<WebCamTexture>
    {
        public WebCamTexture Create(string deviceName, int width, int height, int fps)
        {
            var instance = new WebCamTexture(deviceName, width, height, fps);
            TrackInstance(instance);
            return instance;
        }

        protected override void OnDispose(WebCamTexture instance)
        {
            if (instance.isPlaying)
            {
                instance.Stop();
            }
        }
    }
}
#endif