using UnityEngine;

namespace StreamVideo.Core
{
    //StreamTODO: auto add this component when the video client is created or throw an error if no AudioListener is found
    public class StreamAudioCaptureFilter : MonoBehaviour
    {
        protected void Awake()
        {
            Debug.LogError("Sample rate output: " + AudioSettings.outputSampleRate);
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Unity.WebRTC.WebRTC.ProcessUnityListenerAudioOutput(data, data.Length, channels);
            System.Array.Clear(data, 0, data.Length);
#endif
        }
    }
}

