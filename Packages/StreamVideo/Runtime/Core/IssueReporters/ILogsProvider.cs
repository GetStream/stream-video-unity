namespace StreamVideo.Core.IssueReporters
{
    /// <summary>
    /// Gets logs from the current device.
    /// </summary>
    internal interface ILogsProvider
    {
        string GetLogs();
    }
}