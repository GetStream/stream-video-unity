using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Trace;
using StreamVideo.Libs.Serialization;
using StreamVideo.v1.Sfu.Models;
using Unity.WebRTC;

namespace StreamVideo.Core.Stats
{
    internal class UnityWebRtcStatsCollector : IWebRtcStatsCollector
    {
        private readonly RtcSession _rtcSession;
        private readonly ISerializer _serializer;
        private readonly TracerManager _tracerManager;
        private Dictionary<string, StatSnapshot> _previousPublisherStats = new Dictionary<string, StatSnapshot>();
        private Dictionary<string, StatSnapshot> _previousSubscriberStats = new Dictionary<string, StatSnapshot>();

        private readonly Dictionary<string, Queue<double>> _frameTimeHistory = new Dictionary<string, Queue<double>>();
        private readonly Dictionary<string, Queue<double>> _fpsHistory = new Dictionary<string, Queue<double>>();
        private const int HistorySize = 2;

        /// <summary>
        /// Snapshot of RTCStats data to avoid holding references to native objects that may be freed
        /// </summary>
        private class StatSnapshot
        {
            public string TypeString { get; set; }
            public long Timestamp { get; set; }
            public Dictionary<string, object> Dict { get; set; }
        }

        private static readonly IReadOnlyList<PerformanceStats> EmptyPerformanceStats = new List<PerformanceStats>();
        private const string EmptyJsonArray = "[]";

        public async Task<StatsCollectionResult> CollectAsync(CancellationToken cancellationToken)
        {
            var publisherStats = _rtcSession.Publisher != null
                ? (await _rtcSession.Publisher.GetStatsReportAsync(cancellationToken)).Stats
                : null;

            var subscriberStats = _rtcSession.Subscriber != null
                ? (await _rtcSession.Subscriber.GetStatsReportAsync(cancellationToken)).Stats
                : null;

            var publisherStatsJson = ConvertStatsToJson(publisherStats);
            var subscriberStatsJson = ConvertStatsToJson(subscriberStats);

            // Performance stats MUST be computed before updating _previous*Stats
            // so that deltas span across cycles (seconds apart), not within the same cycle
            var encodeStats = ComputeEncodeStats(publisherStats);
            var decodeStats = ComputeDecodeStats(subscriberStats);

            string rtcStatsJson;
            if (publisherStats != null && subscriberStats != null)
            {
                var publisherDelta = ComputeDeltaCompression(_previousPublisherStats, publisherStats);
                var subscriberDelta = ComputeDeltaCompression(_previousSubscriberStats, subscriberStats);

                //StreamTodo: check later if we can use fixed buffer e.g. list from pool
                var allTraces = new List<object[]>();

                foreach (var tracer in _tracerManager.GetTracers())
                {
                    var slice = tracer.Take();
                    foreach (var record in slice.Snapshot)
                    {
                        allTraces.Add(new object[] { record.Tag, record.Id, record.Data, record.Timestamp });
                    }
                }

                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                allTraces.Add(new object[] { "getstats", "pub", publisherDelta, timestamp });
                allTraces.Add(new object[] { "getstats", "sub", subscriberDelta, timestamp });

                rtcStatsJson = _serializer.Serialize(allTraces, _serializationOptions);
            }
            else
            {
                rtcStatsJson = EmptyJsonArray;
            }

            // Update previous stats AFTER all computations that depend on them
            _previousPublisherStats = CreateStatsSnapshot(publisherStats);
            _previousSubscriberStats = CreateStatsSnapshot(subscriberStats);

            return new StatsCollectionResult
            {
                PublisherStatsJson = publisherStatsJson,
                SubscriberStatsJson = subscriberStatsJson,
                RtcStatsJson = rtcStatsJson,
                EncodeStats = encodeStats,
                DecodeStats = decodeStats,
            };
        }

        internal UnityWebRtcStatsCollector(RtcSession rtcSession, ISerializer serializer, TracerManager tracerManager)
        {
            _rtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _tracerManager = tracerManager ?? throw new ArgumentNullException(nameof(tracerManager));

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
            if (statsDict == null)
            {
                return EmptyJsonArray;
            }

            var statsArray = new List<Dictionary<string, object>>();

            foreach (var statEntry in statsDict)
            {
                var stat = statEntry.Value;
                var statObject = new Dictionary<string, object>();

                // Unity WebRTC returns timestamps in microseconds; convert to milliseconds to match the browser API
                statObject["timestamp"] = stat.Timestamp / 1000;
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
            if (stats == null)
            {
                return new Dictionary<string, StatSnapshot>();
            }

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
        /// Performs delta compression on WebRTC stats, matching Android implementation (StatsTracer.kt).
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

                // Unity WebRTC returns timestamps in microseconds; convert to milliseconds to match the browser API
                var timestampMs = newStat.Timestamp / 1000;
                diff["timestamp"] = timestampMs;
                if (timestampMs > latestTimestamp)
                {
                    latestTimestamp = timestampMs;
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

            var result = new Dictionary<string, object>();
            foreach (var entry in delta)
            {
                result[entry.Key] = entry.Value;
            }
            result["timestamp"] = latestTimestamp;

            return result;
        }

        /// <summary>
        /// Computes encoder performance stats from publisher WebRTC stats, matching Android implementation.
        /// </summary>
        private List<PerformanceStats> ComputeEncodeStats(IDictionary<string, RTCStats> currentStats)
        {
            if (currentStats == null)
            {
                return new List<PerformanceStats>();
            }

            var result = new List<PerformanceStats>();

            foreach (var entry in currentStats)
            {
                var stat = entry.Value;
                if (GetEnumStringValue(stat.Type) != "outbound-rtp") continue;
                if (!GetStringValue(stat.Dict, "kind").Equals("video")) continue;

                if (!_previousPublisherStats.ContainsKey(entry.Key)) continue;

                var prevStat = _previousPublisherStats[entry.Key];

                var totalEncodeTime = GetDoubleValue(stat.Dict, "totalEncodeTime");
                var prevTotalEncodeTime = GetDoubleValue(prevStat.Dict, "totalEncodeTime");
                var framesSent = GetLongValue(stat.Dict, "framesSent");
                var prevFramesSent = GetLongValue(prevStat.Dict, "framesSent");

                var dtEncode = totalEncodeTime - prevTotalEncodeTime;
                var dfSent = framesSent - prevFramesSent;

                var frameTime = dfSent > 0 ? (dtEncode / dfSent) * 1000 : 0.0;

                var statId = entry.Key;
                if (!_frameTimeHistory.ContainsKey(statId))
                {
                    _frameTimeHistory[statId] = new Queue<double>();
                    _fpsHistory[statId] = new Queue<double>();
                }

                _frameTimeHistory[statId].Enqueue(frameTime);
                if (_frameTimeHistory[statId].Count > HistorySize)
                    _frameTimeHistory[statId].Dequeue();

                var fps = GetDoubleValue(stat.Dict, "framesPerSecond");
                _fpsHistory[statId].Enqueue(fps);
                if (_fpsHistory[statId].Count > HistorySize)
                    _fpsHistory[statId].Dequeue();

                var avgFrameTime = _frameTimeHistory[statId].Average();
                var avgFps = _fpsHistory[statId].Average();

                var codecId = GetStringValue(stat.Dict, "codecId");
                var codec = ExtractCodec(currentStats, codecId);

                var perfStat = new PerformanceStats
                {
                    TrackType = TrackType.Video,
                    Codec = codec,
                    AvgFrameTimeMs = (float)avgFrameTime,
                    AvgFps = (float)avgFps,
                    TargetBitrate = GetIntValue(stat.Dict, "targetBitrate"),
                    VideoDimension = new VideoDimension
                    {
                        Width = (uint)GetIntValue(stat.Dict, "frameWidth"),
                        Height = (uint)GetIntValue(stat.Dict, "frameHeight")
                    }
                };

                result.Add(perfStat);
            }

            return result;
        }

        /// <summary>
        /// Computes decoder performance stats from subscriber WebRTC stats, matching Android implementation.
        /// </summary>
        private List<PerformanceStats> ComputeDecodeStats(IDictionary<string, RTCStats> currentStats)
        {
            if (currentStats == null)
            {
                return new List<PerformanceStats>();
            }

            var result = new List<PerformanceStats>();

            RTCStats largestStat = null;
            string largestStatId = null;
            int largestArea = 0;

            foreach (var entry in currentStats)
            {
                var stat = entry.Value;
                if (GetEnumStringValue(stat.Type) != "inbound-rtp") continue;
                if (!GetStringValue(stat.Dict, "kind").Equals("video")) continue;

                var width = GetIntValue(stat.Dict, "frameWidth");
                var height = GetIntValue(stat.Dict, "frameHeight");
                var area = width * height;

                if (area > largestArea)
                {
                    largestArea = area;
                    largestStat = stat;
                    largestStatId = entry.Key;
                }
            }

            if (largestStat == null || !_previousSubscriberStats.ContainsKey(largestStatId))
                return result;

            var prevStat = _previousSubscriberStats[largestStatId];

            var totalDecodeTime = GetDoubleValue(largestStat.Dict, "totalDecodeTime");
            var prevTotalDecodeTime = GetDoubleValue(prevStat.Dict, "totalDecodeTime");
            var framesDecoded = GetLongValue(largestStat.Dict, "framesDecoded");
            var prevFramesDecoded = GetLongValue(prevStat.Dict, "framesDecoded");

            var dtDecode = totalDecodeTime - prevTotalDecodeTime;
            var dfDecoded = framesDecoded - prevFramesDecoded;

            var frameTime = dfDecoded > 0 ? (dtDecode / dfDecoded) * 1000 : 0.0;

            var statId = largestStatId;
            if (!_frameTimeHistory.ContainsKey(statId))
            {
                _frameTimeHistory[statId] = new Queue<double>();
                _fpsHistory[statId] = new Queue<double>();
            }

            _frameTimeHistory[statId].Enqueue(frameTime);
            if (_frameTimeHistory[statId].Count > HistorySize)
                _frameTimeHistory[statId].Dequeue();

            var fps = GetDoubleValue(largestStat.Dict, "framesPerSecond");
            _fpsHistory[statId].Enqueue(fps);
            if (_fpsHistory[statId].Count > HistorySize)
                _fpsHistory[statId].Dequeue();

            var avgFrameTime = _frameTimeHistory[statId].Average();
            var avgFps = _fpsHistory[statId].Average();

            var codecId = GetStringValue(largestStat.Dict, "codecId");
            var codec = ExtractCodec(currentStats, codecId);

            var perfStat = new PerformanceStats
            {
                TrackType = TrackType.Video,
                Codec = codec,
                AvgFrameTimeMs = (float)avgFrameTime,
                AvgFps = (float)avgFps,
                VideoDimension = new VideoDimension
                {
                    Width = (uint)GetIntValue(largestStat.Dict, "frameWidth"),
                    Height = (uint)GetIntValue(largestStat.Dict, "frameHeight")
                }
            };

            result.Add(perfStat);

            return result;
        }

        /// <summary>
        /// Extracts codec information from stats
        /// </summary>
        private Codec ExtractCodec(IDictionary<string, RTCStats> stats, string codecId)
        {
            if (string.IsNullOrEmpty(codecId) || !stats.ContainsKey(codecId))
                return null;

            var codecStat = stats[codecId];
            var mimeType = GetStringValue(codecStat.Dict, "mimeType");
            var clockRate = (uint)GetIntValue(codecStat.Dict, "clockRate");
            var payloadType = (uint)GetIntValue(codecStat.Dict, "payloadType");
            var sdpFmtpLine = GetStringValue(codecStat.Dict, "sdpFmtpLine");

            return new Codec
            {
                Name = mimeType,
                ClockRate = clockRate,
                PayloadType = payloadType,
                Fmtp = sdpFmtpLine
            };
        }

        private static string GetStringValue(IDictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) && value != null ? value.ToString() : string.Empty;
        }

        private static double GetDoubleValue(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is double d) return d;
                if (value is float f) return f;
                if (value is int i) return i;
                if (value is long l) return l;
                if (double.TryParse(value.ToString(), out var result)) return result;
            }
            return 0.0;
        }

        private static long GetLongValue(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is long l) return l;
                if (value is int i) return i;
                if (value is uint ui) return ui;
                if (value is ulong ul) return (long)ul;
                if (long.TryParse(value.ToString(), out var result)) return result;
            }
            return 0L;
        }

        private static int GetIntValue(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is int i) return i;
                if (value is long l) return (int)l;
                if (value is uint ui) return (int)ui;
                if (value is double d) return (int)d;
                if (int.TryParse(value.ToString(), out var result)) return result;
            }
            return 0;
        }
    }
}
