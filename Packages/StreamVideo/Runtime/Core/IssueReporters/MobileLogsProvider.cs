namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
    internal class MobileLogsProvider : ILogsProvider
    {
        public MobileLogsProvider(ILogsCollector logsCollector)
        {
            _logsCollector = logsCollector;
        }

        public string GetLogs() => _logsCollector.GetLogs();

        private readonly ILogsCollector _logsCollector;
    }
#endif
}