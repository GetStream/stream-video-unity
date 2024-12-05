using System.Threading.Tasks;

namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
    /// <summary>
    /// Sends user feedback to a remote service.
    /// </summary>
    internal interface IFeedbackReporter
    {
        public Task SendCallReport(string callId, string participantId);
    }
#endif
}