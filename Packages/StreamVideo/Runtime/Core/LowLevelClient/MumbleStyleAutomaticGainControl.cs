using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Automatic Gain Control (AGC) algorithm inspired by Mumble.
    /// </summary>
    internal class MumbleStyleAutomaticGainControl
    {
        public float CalculateDesiredVolumeGain(float[] data)
        {
            // Find peak
            var peak = 0.0f;
            
            for (int i = 0; i < data.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            // Update max peak with decay
            _maxPeak = Mathf.Max(peak, _maxPeak * PeakDecayRate);

            // Adjust gain
            if (_maxPeak > TargetPeak)
            {
                // Peak too high - reduce gain quickly
                _currentGain = Mathf.Max(MinGain, _currentGain - GainIncreaseRate * (_maxPeak / TargetPeak));
            }
            else
            {
                // Peak too low - increase gain slowly
                _currentGain = Mathf.Min(MaxGain, _currentGain + GainDecayRate);
            }

            return _currentGain;
        }

        private const float TargetPeak = 0.15f;
        private const float GainIncreaseRate = 0.1f;
        private const float GainDecayRate = 0.12f;
        private const float PeakDecayRate = 0.999f;
        private const float MaxGain = 4.0f;
        private const float MinGain = 1f;

        private float _currentGain = 3.0f;
        private float _maxPeak;
    }
}