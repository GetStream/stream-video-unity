using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.v1.Sfu.Models;

namespace StreamVideo.Core.Stats
{
    internal class StatsCollectionResult
    {
        public string PublisherStatsJson { get; set; }
        public string SubscriberStatsJson { get; set; }
        public string RtcStatsJson { get; set; }
        public IReadOnlyList<PerformanceStats> EncodeStats { get; set; }
        public IReadOnlyList<PerformanceStats> DecodeStats { get; set; }
    }

    internal interface IWebRtcStatsCollector
    {
        Task<StatsCollectionResult> CollectAsync(CancellationToken cancellationToken);
    }
}
