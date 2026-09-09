using System;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
    internal class FeedbackReporterFactory
    {
        public FeedbackReporterFactory(ILogsCollector logsCollector, ISerializer serializer, ILogs logs)
        {
            _logsCollector = logsCollector ?? throw new ArgumentNullException(nameof(logsCollector));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        }

        public IFeedbackReporter CreateTrelloReporter()
        {
            var logsProvider = CreateLogsProvider();
            return new TrelloFeedbackReporter(logsProvider, _logs);
        }

        private readonly ILogsCollector _logsCollector;
        private readonly ISerializer _serializer;
        private readonly ILogs _logs;

        private ILogsProvider CreateLogsProvider()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return new MobileLogsProvider(_logsCollector);
#elif UNITY_STANDALONE || UNITY_EDITOR
            return new StandaloneLogsProvider();
#else
            // WebGL and other platforms have no player.log file. Use the in-session collector instead of failing client construction.
            return new MobileLogsProvider(_logsCollector);
#endif
        }
    }
#endif
}