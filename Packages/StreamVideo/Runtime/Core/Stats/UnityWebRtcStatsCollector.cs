using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Libs.Serialization;
using Unity.WebRTC;

namespace StreamVideo.Core.Stats
{
    internal class UnityWebRtcStatsCollector : IWebRtcStatsCollector
    {
        private readonly RtcSession _rtcSession;
        private readonly ISerializer _serializer;


        public async Task<string> GetPublisherStatsJsonAsync()
        {
            var report = await _rtcSession.Publisher.GetStatsReportAsync();
            var stats = ToStatsArray(report.Stats);
            return _serializer.Serialize(stats, _serializationOptions);
        }

        public async Task<string> GetSubscriberStatsJsonAsync()
        {
            var report = await _rtcSession.Subscriber.GetStatsReportAsync();
            var stats = ToStatsArray(report.Stats);
            return _serializer.Serialize(stats, _serializationOptions);
        }

        internal UnityWebRtcStatsCollector(RtcSession rtcSession, ISerializer serializer)
        {
            _rtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            _serializationOptions = new SerializationOptions
            {
                IgnorePropertyHandler = ShouldIgnoredPropertyFromSerialization,
                Converters = new ISerializationConverter[]
                {
                    new EnumValueConverter()
                }
            };
        }

        private readonly SerializationOptions _serializationOptions;

        private static RTCStats[] ToStatsArray(IDictionary<string, RTCStats> statsDict)
            => statsDict.Select(d => d.Value).ToArray();

        // Each stats object contains an internal dictionary that contains all the data,
        // but this data is already included in the properties of each object
        private static bool ShouldIgnoredPropertyFromSerialization(MemberInfo memberInfo)
            => memberInfo.Name == nameof(RTCStats.Dict) || memberInfo.Name == nameof(RTCStats.UtcTimeStamp);
    }
}