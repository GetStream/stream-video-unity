namespace StreamVideo.Core
{
    /// <summary>
        /// Why a background filter was marked degraded.
    /// </summary>
    public enum BackgroundFilterDegradeReason
    {
        None = 0,
        FrameDrop = 1,
        CpuThrottling = 2,
    }
}
