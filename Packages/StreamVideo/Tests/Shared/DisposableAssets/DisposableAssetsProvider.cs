#if STREAM_TESTS_ENABLED
namespace StreamVideo.Tests.Shared.DisposableAssets
{
    public class DisposableAssetsProvider
    {
        public WebCamTextureFactory WebCamTextureFactory { get; } = new WebCamTextureFactory();
        
        public void DisposeInstances()
        {
            WebCamTextureFactory.DisposeInstances();
        }
    }
}
#endif