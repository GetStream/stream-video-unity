using System.Threading.Tasks;

namespace StreamVideo.Core.IssueReporters
{
    /// <summary>
    /// Sends user feedback to a remove service.
    /// </summary>
    internal interface IFeedbackReporter
    {
        public Task SendCallReport(string callId, string participantId);
    }
}