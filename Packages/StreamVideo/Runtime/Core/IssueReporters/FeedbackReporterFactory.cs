using System;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.IssueReporters
{
    internal class FeedbackReporterFactory
    {
        public FeedbackReporterFactory(ILogsCollector logsCollector, ISerializer serializer)
        {
            _logsCollector = logsCollector ?? throw new ArgumentNullException(nameof(logsCollector));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IFeedbackReporter CreateTrelloReporter()
        {
            var logsProvider = CreateLogsProvider();
            return new TrelloFeedbackReporter(logsProvider, _serializer);
        }

        private readonly ILogsCollector _logsCollector;
        private readonly ISerializer _serializer;

        private ILogsProvider CreateLogsProvider()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return new MobileLogsProvider(_logsCollector);
#elif UNITY_STANDALONE || UNITY_EDITOR
            return new StandaloneLogsProvider();
#else
            throw new System.NotSupportedException($"Logs provider for platform {UnityEngine.Application.platform} is not supported");
#endif
        }
    }
}