using System.Threading.Tasks;

namespace StreamVideo.Core.Stats
{
    internal interface IWebRtcStatsCollector
    {
        Task<string> GetPublisherStatsJsonAsync();

        Task<string> GetSubscriberStatsJsonAsync();
    }
}