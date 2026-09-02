using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    internal sealed class NullPersonSegmenter : IPersonSegmenter
    {
        public bool IsSupported => false;

        public bool HasMask => false;

        public Texture MaskTexture => null;

        public void RequestSegmentation(Texture source)
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Dispose()
        {
        }
    }
}
