namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Requests a mask whenever the previous request is done. Native busy flags are the
    /// real throttle; the interval is only a floor (1 = every Unity frame).
    /// Degrades on sustained low processed/source FPS.
    /// Hysteresis: degrade below 0.75, recover above 0.85.
    /// </summary>
    internal sealed class FilterFrameScheduler
    {
        public const int DefaultSegmentIntervalFrames = 1;
        public const float DegradeRatioThreshold = 0.75f;
        public const float RecoverRatioThreshold = 0.85f;
        public const float DegradeHoldSeconds = 3f;

        public int SegmentIntervalFrames { get; private set; } = DefaultSegmentIntervalFrames;

        public BlurIntensity EffectiveIntensity { get; private set; } = BlurIntensity.Heavy;

        public bool ShouldDisable { get; private set; }

        public BackgroundFilterPerformance Performance { get; private set; }
            = new BackgroundFilterPerformance(false, BackgroundFilterDegradeReason.None);

        public int QualityTier { get; private set; }

        public void Reset(BlurIntensity requestedIntensity)
        {
            _requestedIntensity = requestedIntensity;
            EffectiveIntensity = requestedIntensity;
            SegmentIntervalFrames = DefaultSegmentIntervalFrames;
            QualityTier = 0;
            ShouldDisable = false;
            _lowRatioSeconds = 0f;
            _highRatioSeconds = 0f;
            Performance = new BackgroundFilterPerformance(false, BackgroundFilterDegradeReason.None);
        }

        public bool ShouldSegment(int frameIndex)
            => frameIndex % SegmentIntervalFrames == 0;

        /// <summary>
        /// Feed processed/source FPS ratio (1 = keeping up). <paramref name="deltaSeconds"/> is the sample window.
        /// </summary>
        public void RecordFpsRatio(float processedToSourceFpsRatio, float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (processedToSourceFpsRatio < DegradeRatioThreshold)
            {
                _lowRatioSeconds += deltaSeconds;
                _highRatioSeconds = 0f;
                if (_lowRatioSeconds >= DegradeHoldSeconds)
                {
                    _lowRatioSeconds = 0f;
                    Degrade();
                }

                return;
            }

            if (processedToSourceFpsRatio > RecoverRatioThreshold)
            {
                _highRatioSeconds += deltaSeconds;
                _lowRatioSeconds = 0f;
                if (_highRatioSeconds >= DegradeHoldSeconds)
                {
                    _highRatioSeconds = 0f;
                    Recover();
                }

                return;
            }

            _lowRatioSeconds = 0f;
            _highRatioSeconds = 0f;
        }

        private const int MaxQualityTier = 3;

        private BlurIntensity _requestedIntensity = BlurIntensity.Heavy;
        private float _lowRatioSeconds;
        private float _highRatioSeconds;

        private void Degrade()
        {
            if (QualityTier >= MaxQualityTier)
            {
                return;
            }

            QualityTier++;
            ApplyTier();
        }

        private void Recover()
        {
            if (QualityTier <= 0)
            {
                return;
            }

            QualityTier--;
            ApplyTier();
        }

        private void ApplyTier()
        {
            switch (QualityTier)
            {
                case 0:
                    SegmentIntervalFrames = DefaultSegmentIntervalFrames;
                    EffectiveIntensity = _requestedIntensity;
                    ShouldDisable = false;
                    Performance = new BackgroundFilterPerformance(false, BackgroundFilterDegradeReason.None);
                    break;
                case 1:
                    SegmentIntervalFrames = 2;
                    EffectiveIntensity = _requestedIntensity;
                    ShouldDisable = false;
                    Performance = new BackgroundFilterPerformance(true, BackgroundFilterDegradeReason.FrameDrop);
                    break;
                case 2:
                    SegmentIntervalFrames = 3;
                    EffectiveIntensity = BlurIntensity.Light;
                    ShouldDisable = false;
                    Performance = new BackgroundFilterPerformance(true, BackgroundFilterDegradeReason.CpuThrottling);
                    break;
                default:
                    SegmentIntervalFrames = 3;
                    EffectiveIntensity = BlurIntensity.Light;
                    ShouldDisable = true;
                    Performance = new BackgroundFilterPerformance(true, BackgroundFilterDegradeReason.FrameDrop);
                    break;
            }
        }
    }
}
