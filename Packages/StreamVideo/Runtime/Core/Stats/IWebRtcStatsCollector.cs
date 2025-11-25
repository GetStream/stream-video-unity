using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.v1.Sfu.Models;

namespace StreamVideo.Core.Stats
{
    internal interface IWebRtcStatsCollector
    {
        Task<string> GetPublisherStatsJsonAsync(CancellationToken cancellationToken);

        Task<string> GetSubscriberStatsJsonAsync(CancellationToken cancellationToken);

        Task<string> GetRtcStatsJsonAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<PerformanceStats>> GetEncodeStatsAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<PerformanceStats>> GetDecodeStatsAsync(CancellationToken cancellationToken);
    }
}