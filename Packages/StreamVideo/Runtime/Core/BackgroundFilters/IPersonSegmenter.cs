using System;
using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    internal interface IPersonSegmenter : IDisposable
    {
        bool IsSupported { get; }

        bool HasMask { get; }

        Texture MaskTexture { get; }

        /// <summary>
        /// Request a new mask for <paramref name="source"/>. Must not block. Reuse the last mask if busy.
        /// </summary>
        void RequestSegmentation(Texture source);

        /// <summary>
        /// Drain a completed native mask onto the Unity texture. No-op when the backend is not async.
        /// </summary>
        void PumpPendingMask();

        void Pause();

        void Resume();
    }
}
