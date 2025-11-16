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
        private Dictionary<string, StatSnapshot> _previousPublisherStats = new Dictionary<string, StatSnapshot>();
        private Dictionary<string, StatSnapshot> _previousSubscriberStats = new Dictionary<string, StatSnapshot>();

        /// <summary>
        /// Snapshot of RTCStats data to avoid holding references to native objects that may be freed
        /// </summary>
        private class StatSnapshot
        {
            public string TypeString { get; set; }
            public long Timestamp { get; set; }
            public Dictionary<string, object> Dict { get; set; }
        }

        public async Task<string> GetPublisherStatsJsonAsync()
        {
            var report = await _rtcSession.Publisher.GetStatsReportAsync();
            return ConvertStatsToJson(report.Stats);
        }

        public async Task<string> GetSubscriberStatsJsonAsync()
        {
            var report = await _rtcSession.Subscriber.GetStatsReportAsync();
            return ConvertStatsToJson(report.Stats);
        }

        public async Task<string> GetRtcStatsJsonAsync()
        {
            // Get both publisher and subscriber stats
            var publisherReport = await _rtcSession.Publisher.GetStatsReportAsync();
            var subscriberReport = await _rtcSession.Subscriber.GetStatsReportAsync();

            // Compute delta-compressed stats
            var publisherDelta = ComputeDeltaCompression(_previousPublisherStats, publisherReport.Stats);
            var subscriberDelta = ComputeDeltaCompression(_previousSubscriberStats, subscriberReport.Stats);

            // Update previous stats for next delta - create snapshots to avoid holding native references
            _previousPublisherStats = CreateStatsSnapshot(publisherReport.Stats);
            _previousSubscriberStats = CreateStatsSnapshot(subscriberReport.Stats);

            // Create trace records in the format: ["tag", "id", data, timestamp]
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var traces = new List<object[]>
            {
                new object[] { "getstats", "pub", publisherDelta, timestamp },
                new object[] { "getstats", "sub", subscriberDelta, timestamp }
            };

            return _serializer.Serialize(traces, _serializationOptions);
        }

        internal UnityWebRtcStatsCollector(RtcSession rtcSession, ISerializer serializer)
        {
            _rtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            _serializationOptions = new SerializationOptions
            {
                Converters = new ISerializationConverter[]
                {
                    new EnumValueConverter()
                },
                NamingStrategy = SerializationNamingStrategy.CamelCase
            };
        }

        private readonly SerializationOptions _serializationOptions;

        /// <summary>
        /// Converts RTCStats dictionary to JSON format matching Android implementation and rtcstats dump-importer expectations.
        /// Each stat is converted to an object with: timestamp, id, type, and all member properties.
        /// </summary>
        private string ConvertStatsToJson(IDictionary<string, RTCStats> statsDict)
        {
            var statsArray = new List<Dictionary<string, object>>();

            foreach (var statEntry in statsDict)
            {
                var stat = statEntry.Value;
                var statObject = new Dictionary<string, object>();

                // Add required fields: timestamp (in microseconds), id, and type
                statObject["timestamp"] = stat.Timestamp;
                statObject["id"] = stat.Id;
                
                // Convert the enum to its string representation using the StringValueAttribute
                statObject["type"] = GetEnumStringValue(stat.Type);

                // Add all member properties from the Dict
                foreach (var member in stat.Dict)
                {
                    statObject[member.Key] = member.Value;
                }

                statsArray.Add(statObject);
            }

            return _serializer.Serialize(statsArray, _serializationOptions);
        }

        /// <summary>
        /// Gets the string value for an enum using the StringValueAttribute, matching the EnumValueConverter logic.
        /// </summary>
        private static string GetEnumStringValue(Enum enumValue)
        {
            var type = enumValue.GetType();
            var fieldInfo = type.GetField(enumValue.ToString());
            
            if (fieldInfo == null)
            {
                return enumValue.ToString();
            }

            var attribute = (StringValueAttribute)fieldInfo.GetCustomAttribute(typeof(StringValueAttribute));
            return attribute?.StringValue ?? enumValue.ToString();
        }

        /// <summary>
        /// Creates a snapshot of RTCStats to avoid holding references to native objects that may be freed.
        /// </summary>
        private Dictionary<string, StatSnapshot> CreateStatsSnapshot(IDictionary<string, RTCStats> stats)
        {
            var snapshot = new Dictionary<string, StatSnapshot>();
            foreach (var entry in stats)
            {
                snapshot[entry.Key] = new StatSnapshot
                {
                    TypeString = GetEnumStringValue(entry.Value.Type),
                    Timestamp = entry.Value.Timestamp,
                    Dict = new Dictionary<string, object>(entry.Value.Dict)
                };
            }
            return snapshot;
        }

        /// <summary>
        /// Performs delta compression on WebRTC stats, matching Android implementation.
        /// Only includes changed values and normalizes timestamps.
        /// </summary>
        private Dictionary<string, object> ComputeDeltaCompression(
            IDictionary<string, StatSnapshot> oldStats, 
            IDictionary<string, RTCStats> newStats)
        {
            var delta = new Dictionary<string, Dictionary<string, object>>();
            long latestTimestamp = 0;

            // Build per-report diffs
            foreach (var entry in newStats)
            {
                var id = entry.Key;
                var newStat = entry.Value;
                var diff = new Dictionary<string, object>();

                StatSnapshot oldStat = null;
                oldStats?.TryGetValue(id, out oldStat);

                // Add type if it's new or changed
                var typeString = GetEnumStringValue(newStat.Type);
                if (oldStat == null || oldStat.TypeString != typeString)
                {
                    diff["type"] = typeString;
                }

                // Compare members
                foreach (var member in newStat.Dict)
                {
                    object oldValue = null;
                    var hasOldValue = oldStat?.Dict.TryGetValue(member.Key, out oldValue) ?? false;
                    if (!hasOldValue || !Equals(member.Value, oldValue))
                    {
                        diff[member.Key] = member.Value;
                    }
                }

                // Add timestamp (in microseconds) for later normalization
                diff["timestamp"] = newStat.Timestamp;
                if (newStat.Timestamp > latestTimestamp)
                {
                    latestTimestamp = newStat.Timestamp;
                }

                delta[id] = diff;
            }

            // Normalize timestamps: set the latest timestamp to 0 in the reports
            foreach (var report in delta.Values)
            {
                if (report.TryGetValue("timestamp", out var ts) && ts is long timestamp)
                {
                    if (timestamp == latestTimestamp)
                    {
                        report["timestamp"] = 0L;
                    }
                }
            }

            // Create result with delta and top-level timestamp (in milliseconds)
            var result = new Dictionary<string, object>();
            foreach (var entry in delta)
            {
                result[entry.Key] = entry.Value;
            }
            result["timestamp"] = latestTimestamp / 1000; // Convert microseconds to milliseconds

            return result;
        }
    }
}