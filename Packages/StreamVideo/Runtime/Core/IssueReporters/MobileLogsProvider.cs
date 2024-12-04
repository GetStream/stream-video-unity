namespace StreamVideo.Core.IssueReporters
{
    internal class MobileLogsProvider : ILogsProvider
    {
        public MobileLogsProvider(ILogsCollector logsCollector)
        {
            _logsCollector = logsCollector;
        }

        public string GetLogs() => _logsCollector.GetLogs();

        private readonly ILogsCollector _logsCollector;
    }
}