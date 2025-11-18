using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.v1.Sfu.Models;

namespace StreamVideo.Core.Stats
{
    internal interface IWebRtcStatsCollector
    {
        Task<string> GetPublisherStatsJsonAsync();

        Task<string> GetSubscriberStatsJsonAsync();

        Task<string> GetRtcStatsJsonAsync();

        Task<IReadOnlyList<PerformanceStats>> GetEncodeStatsAsync();

        Task<IReadOnlyList<PerformanceStats>> GetDecodeStatsAsync();
    }
}