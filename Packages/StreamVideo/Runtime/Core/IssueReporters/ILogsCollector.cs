using System;

namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
    /// <summary>
    /// Collects logs when enabled
    /// </summary>
    internal interface ILogsCollector : IDisposable
    {
        string GetLogs();

        void Enable();

        void Disable();
    }
#endif
}