using System;

namespace StreamVideo.Core.IssueReporters
{
    /// <summary>
    /// Collects logs when enabled
    /// </summary>
    internal interface ILogsCollector : IDisposable
    {
        string GetLogs();

        void Enable();

        void Disable();
    }
}