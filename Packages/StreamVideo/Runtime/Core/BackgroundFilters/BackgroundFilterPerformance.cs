namespace StreamVideo.Core
{
    /// <summary>
    /// Performance snapshot for the active background filter.
    /// </summary>
    public readonly struct BackgroundFilterPerformance
    {
        public BackgroundFilterPerformance(bool degraded, BackgroundFilterDegradeReason reason)
        {
            Degraded = degraded;
            Reason = reason;
        }

        /// <summary>
        /// True when the filter dropped quality or disabled itself to protect frame rate.
        /// </summary>
        public bool Degraded { get; }

        /// <summary>
        /// Why the filter is degraded, or <see cref="BackgroundFilterDegradeReason.None"/>.
        /// </summary>
        public BackgroundFilterDegradeReason Reason { get; }
    }
}
