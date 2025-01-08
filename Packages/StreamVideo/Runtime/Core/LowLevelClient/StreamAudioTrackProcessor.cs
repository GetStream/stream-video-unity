using Unity.WebRTC;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    internal class AudioNoiseSuppressor
    {
        public AudioNoiseSuppressor(int movingAverageWindow = 3)
        {
            _previousSamples = new float[movingAverageWindow];
        }
        
        public float[] ProcessAudio(float[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                // First apply noise gate
                var sample = Mathf.Abs(data[i]) < NoiseGateThreshold ? 0f : data[i];
            
                // Then apply moving average
                // Shift previous samples
                for (var j = _previousSamples.Length - 1; j > 0; j--)
                {
                    _previousSamples[j] = _previousSamples[j-1];
                }
                _previousSamples[0] = sample;
            
                // Calculate average
                float sum = 0;
                for (var j = 0; j < _previousSamples.Length; j++)
                {
                    sum += _previousSamples[j];
                }
            
                data[i] = sum / _previousSamples.Length;
            }
        
            return data;
        }
        
        private const float NoiseGateThreshold = 0.00002f;

        // Adjust this for more/less smoothing
        private const int MovingAverageWindow = 3;
        
        private readonly float[] _previousSamples;
    }
    /// <summary>
    /// This component takes audio chunks from an <see cref="AudioSource"/> and transfers them to an <see cref="AudioStreamTrack"/>.
    /// It also applies AGC (Automatic Gain Control) to the audio in order to make the audio output louder.
    /// </summary>
    public class StreamAudioTrackProcessor : MonoBehaviour
    {
        public void SetTarget(AudioStreamTrack audioStreamTrack) => _audioStreamTrack = audioStreamTrack;

        public void OnAudioFilterRead(float[] data, int channels)
        {
            if (_audioStreamTrack == null)
            {
                return;
            }

            var volumeGain = _agc.CalculateDesiredVolumeGain(data);
#if STREAM_DEBUG_ENABLED
            _debugPrinter.OnAudioFilterReadStart(volumeGain);
#endif
            
            data = _audioNoiseSuppressor.ProcessAudio(data);

            for (var i = 0; i < data.Length; i++)
            {
                var newValue = volumeGain * data[i];
#if STREAM_DEBUG_ENABLED
                _debugPrinter.OnAudioFilterValueStep(data[i], newValue);
#endif

                data[i] = Mathf.Clamp(newValue, -1f, 1f);
            }

            if (_audioStreamTrack != null)
            {
                _audioStreamTrack.SetData(data, channels, _sampleRate);
            }

            // This removes echo from the audio. Echo is related to how much gain we apply. Without any gain there is no echo.
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
#if STREAM_DEBUG_ENABLED
            _debugPrinter.OnAudioFilterReadEnd(data, channels);
#endif
        }
        
        private readonly MumbleStyleAGC _agc = new MumbleStyleAGC();
        private readonly AudioNoiseSuppressor _audioNoiseSuppressor = new AudioNoiseSuppressor();
        private readonly DebugPrinter _debugPrinter = new DebugPrinter();

        private AudioStreamTrack _audioStreamTrack;
        private int _sampleRate;

        private class DebugPrinter
        {
            /// <summary>
            /// Audio thread
            /// </summary>
            public void OnAudioFilterReadStart(float gain)
            {
                _lastGain = gain;

                _originalSum = 0;
                _highest = 0;
                _lowest = 0;
            }

            /// <summary>
            /// Audio thread
            /// </summary>
            public void OnAudioFilterValueStep(float originalValue, float newValue)
            {
                _originalSum += originalValue;
                if (newValue < -1 || newValue > 1)
                {
                    _clampingLostPrecision = true;
                }

                if (newValue > _highest)
                {
                    _highest = newValue;
                }

                if (newValue < _lowest)
                {
                    _lowest = newValue;
                }
            }

            /// <summary>
            /// Audio thread
            /// </summary>
            public void OnAudioFilterReadEnd(float[] data, int channels)
            {
                _originalAvg = _originalSum / data.Length;

                if (Mathf.Abs(_originalSum) < 0.000000001f)
                {
                    _lastFrameWasSilent = true;
                }
            }

            /// <summary>
            /// Main thread
            /// </summary>
            public void PrintDebugInfo()
            {
                if (_lastFrameWasSilent)
                {
                    _lastFrameWasSilent = false;
                    Debug.LogWarning(
                        $"Last frame was silent. Avg: {_originalAvg}, Highest: {_highest}, Lowest: {_lowest}, Gain: {_lastGain}");
                }
                else
                {
                    Debug.Log(
                        $"Last frame had output. Avg: {_originalAvg}, Highest: {_highest}, Lowest: {_lowest}, Gain: {_lastGain}");
                }

                if (_clampingLostPrecision)
                {
                    Debug.LogError("Clamping lost precision!");
                    _clampingLostPrecision = false;
                }
            }

            private float _lastGain;

            private bool _lastFrameWasSilent;
            private bool _clampingLostPrecision;

            private float _originalSum;
            private float _originalAvg;
            private float _highest;
            private float _lowest;
        }

        void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
        }
#if STREAM_DEBUG_ENABLED
        void Update()
        {
            if (!_audioSource.isPlaying)
            {
                return;
            }

            _debugPrinter.PrintDebugInfo();
        }
#endif
    }
}