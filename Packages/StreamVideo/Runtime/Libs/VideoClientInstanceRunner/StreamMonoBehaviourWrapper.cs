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
        /// This is a MonoBehaviour wrapper that will pass Unity Engine callbacks to the Stream Video Client
        /// </summary>
        public sealed class UnityStreamVideoClientRunner : MonoBehaviour, IStreamVideoClientRunner
        {
            public void RunClientInstance(IStreamVideoClientEventsListener streamVideoInstance)
            {
                if (!Application.isPlaying)
                {
                    Debug.LogWarning($"Application is not playing. The MonoBehaviour {nameof(UnityStreamVideoClientRunner)} wrapper will not execute." +
                              $" You need to call Stream Video Client's {nameof(IStreamVideoClientEventsListener.Update)} and {nameof(IStreamVideoClientEventsListener.Destroy)} by yourself");
                    DestroyImmediate(gameObject);
                    return;
                }
                
                _streamVideoInstance = streamVideoInstance ?? throw new ArgumentNullException(nameof(streamVideoInstance));
                _streamVideoInstance.Destroyed += OnStreamVideoInstanceDestroyed;
                StartCoroutine(UpdateCoroutine());
                
                //StreamTodo: should not be needed in the future thanks to this PR: https://github.com/Unity-Technologies/com.unity.webrtc/pull/977
                StartCoroutine(streamVideoInstance.WebRTCUpdateCoroutine());
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

                _streamVideoInstance.Destroyed -= OnStreamVideoInstanceDestroyed;
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

            private void OnStreamVideoInstanceDestroyed()
            {
                if (_streamVideoInstance == null)
                {
                    return;
                }

                _streamVideoInstance.Destroyed -= OnStreamVideoInstanceDestroyed;
                _streamVideoInstance = null;
                StopCoroutine(UpdateCoroutine());

#if STREAM_DEBUG_ENABLED
                Debug.Log($"Stream Video Client Disposed - destroy {nameof(UnityStreamVideoClientRunner)} instance");
#endif
                Destroy(gameObject);
            }

        }
    }
}