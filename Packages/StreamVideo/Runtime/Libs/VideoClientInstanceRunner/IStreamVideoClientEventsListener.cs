using System;
using System.Collections;

namespace StreamVideo.Libs.VideoClientInstanceRunner
{
    /// <summary>
    /// Stream Chat Client requires Environment to call <see cref="Update"/> per frame and <see cref="Destroy"/> when application is destroyed
    /// E.g. in Unity Engine a wrapping MonoBehaviour would call them when receiving Update and OnDestroy callbacks respectively
    /// </summary>
    public interface IStreamVideoClientEventsListener
    {
        /// <summary>
        /// Event fired when the client is destroyed
        /// </summary>
        event Action Destroyed;

        /// <summary>
        /// Call when application is being destroyed.
        /// E.g. for Unity call when MonoBehaviour.OnDestroy is called by the engine
        /// </summary>
        void Destroy();

        /// <summary>
        /// Call per application frame update. Calling Update every frame is critical for Stream Chat Client to work properly
        /// E.g. for Unity call when MonoBehaviour.Update is called by the engine or call from coroutine.
        /// </summary>
        void Update();

        /// <summary>
        /// This method exposes the WebRTC.Update(). In Unity, call it once with StartCoroutine(instance.WebRTCUpdateCoroutine());
        /// </summary>
        IEnumerator WebRTCUpdateCoroutine();
    }
}