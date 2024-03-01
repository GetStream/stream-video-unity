#if STREAM_TESTS_ENABLED
using System.Collections.Generic;

namespace StreamVideo.Tests.Shared.DisposableAssets
{
    public abstract class DisposableAssetFactory<TType>
    {
        public void DisposeInstances()
        {
            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                OnDispose(_instances[i]);
            }
            
            _instances.Clear();
        }
        
        protected void TrackInstance(TType type) => _instances.Add(type);

        protected abstract void OnDispose(TType instance);
        
        private readonly List<TType> _instances = new List<TType>();
    }
}
#endif