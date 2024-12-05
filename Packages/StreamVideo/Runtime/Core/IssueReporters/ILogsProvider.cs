namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
    /// <summary>
    /// Gets logs from the current device.
    /// </summary>
    internal interface ILogsProvider
    {
        string GetLogs();
    }
#endif
}