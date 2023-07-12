using System;
using System.Collections;
using UnityEngine;

namespace StreamVideo.Libs.VideoClientInstanceRunner
{
    /// <summary>
    /// Wrapper to hide the <see cref="UnityStreamVideoClientRunner"/> from Unity's inspector dropdowns and Unity search functions like Object.FindObjectsOfType<MonoBehaviour>(); 
    /// </summary>
    public sealed class StreamMonoBehaviourWrapper
    {
        /// <summary>
        /// This is a MonoBehaviour wrapper that will pass Unity Engine callbacks to the Stream Chat Client
        /// </summary>
        public sealed class UnityStreamVideoClientRunner : MonoBehaviour, IStreamVideoClientRunner
        {
            public void RunChatInstance(IStreamVideoClientEventsListener streamVideoInstance)
            {
                if (!Application.isPlaying)
                {
                    Debug.LogWarning($"Application is not playing. The MonoBehaviour {nameof(UnityStreamVideoClientRunner)} wrapper will not execute." +
                              $" You need to call Stream Chat Client's {nameof(IStreamVideoClientEventsListener.Update)} and {nameof(IStreamVideoClientEventsListener.Destroy)} by yourself");
                    DestroyImmediate(gameObject);
                    return;
                }
                
                _streamVideoInstance = streamVideoInstance ?? throw new ArgumentNullException(nameof(streamVideoInstance));
                _streamVideoInstance.Disposed += OnStreamVideoInstanceDisposed;
                StartCoroutine(UpdateCoroutine());
            }

            private IStreamVideoClientEventsListener _streamVideoInstance;
            
            // Called by Unity
            private void Awake()
            {
                DontDestroyOnLoad(gameObject);
            }

            // Called by Unity
            private void OnDestroy()
            {
                if (_streamVideoInstance == null)
                {
                    return;
                }

                _streamVideoInstance.Disposed -= OnStreamVideoInstanceDisposed;
                StopCoroutine(UpdateCoroutine());
                _streamVideoInstance.Destroy();
                _streamVideoInstance = null;
            }

            private IEnumerator UpdateCoroutine()
            {
                while (_streamVideoInstance != null)
                {
                    _streamVideoInstance.Update();
                    yield return null;
                }
            }

            private void OnStreamVideoInstanceDisposed()
            {
                if (_streamVideoInstance == null)
                {
                    return;
                }

                _streamVideoInstance.Disposed -= OnStreamVideoInstanceDisposed;
                _streamVideoInstance = null;
                StopCoroutine(UpdateCoroutine());

#if STREAM_DEBUG_ENABLED
                Debug.Log($"Stream Chat Client Disposed - destroy {nameof(UnityStreamChatClientRunner)} instance");
#endif
                Destroy(gameObject);
            }

        }
    }
}