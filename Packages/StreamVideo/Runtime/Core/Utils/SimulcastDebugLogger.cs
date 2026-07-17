#if STREAM_DEBUG_ENABLED
using System.Collections.Generic;
using System.Text;
using StreamVideo.Core;
using StreamVideo.Libs.Logs;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;
using Unity.WebRTC;
using SfuEvents = StreamVideo.v1.Sfu.Events;

namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// Grep logs with "[Simulcast]" to compare publisher/subscriber devices during simulcast testing.
    /// Requires STREAM_DEBUG_ENABLED (Stream Video SDK debug mode in Project Settings).
    /// </summary>
    internal static class SimulcastDebugLogger
    {
        private const string Tag = "[Simulcast]";

        public static void LogPublisherTrackCreated(ILogs logs, string context,
            VideoResolution? webcamResolution, VideoResolution publishResolution, RTCRtpSendParameters parameters)
        {
            var webcam = webcamResolution.HasValue
                ? FormatResolution(webcamResolution.Value)
                : "none";
            logs.Warning(
                $"{Tag}[Publisher] Track created ({context}). webcam={webcam}, publishTarget={FormatResolution(publishResolution)}, encodings=[{FormatEncodings(parameters)}]");
        }

        public static void LogPublisherAnnouncedLayers(ILogs logs, string context, IEnumerable<VideoLayer> layers)
        {
            var sb = new StringBuilder();
            sb.Append($"{Tag}[Publisher] Announced layers to SFU ({context}): ");
            foreach (var layer in layers)
            {
                sb.Append($"{layer.Rid}={layer.VideoDimension.Width}x{layer.VideoDimension.Height} ");
            }

            logs.Warning(sb.ToString().TrimEnd());
        }

        public static void LogChangePublishQualityRequest(ILogs logs, string context,
            SfuEvents.VideoSender videoSenderSettings)
        {
            var sb = new StringBuilder();
            sb.Append($"{Tag}[Publisher] SFU ChangePublishQuality ({context}): ");

            foreach (var layer in videoSenderSettings.Layers)
            {
                sb.Append($"{layer.Name}=");
                if (!layer.Active)
                {
                    sb.Append("OFF | ");
                    continue;
                }

                sb.Append($"ON br={FormatBitrate(layer.MaxBitrate)} fps={layer.MaxFramerate} sc={layer.ScaleResolutionDownBy} | ");
            }

            TrimTrailingSeparator(sb, " | ");
            logs.Warning(sb.ToString());
        }

        public static void LogPublisherEncodingState(ILogs logs, string context, string phase,
            RTCRtpSendParameters parameters)
        {
            logs.Warning(
                $"{Tag}[Publisher] Encoding state {phase} ({context}): [{FormatEncodings(parameters)}]");
        }

        public static void LogSubscriberConsumeRequest(ILogs logs, string context, string remoteUserId,
            string remoteSessionId, VideoResolution requestedResolution)
        {
            logs.Warning(
                $"{Tag}[Subscriber] Consume request ({context}). remoteUser={remoteUserId}, remoteSession={remoteSessionId}, requestedDimension={FormatResolution(requestedResolution)}");
        }

        public static void LogSubscriberSubscriptions(ILogs logs, string context,
            IEnumerable<TrackSubscriptionDetails> tracks)
        {
            var sb = new StringBuilder();
            sb.Append($"{Tag}[Subscriber] UpdateSubscriptions ({context}): ");

            var hasVideo = false;
            foreach (var track in tracks)
            {
                if (track.TrackType != TrackType.Video)
                {
                    continue;
                }

                hasVideo = true;
                var dimension = track.Dimension != null
                    ? $"{track.Dimension.Width}x{track.Dimension.Height}"
                    : "default";
                sb.Append(
                    $"video(user={track.UserId}, session={track.SessionId}, requestedDim={dimension}); ");
            }

            if (!hasVideo)
            {
                sb.Append("no video subscriptions");
            }
            else
            {
                TrimTrailingSeparator(sb, "; ");
            }

            logs.Warning(sb.ToString());
        }

        public static void LogPublisherOutboundStats(ILogs logs, string context,
            IDictionary<string, RTCStats> stats)
        {
            if (stats == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"{Tag}[Stats][Publisher] outbound-rtp ({context}): ");

            var count = 0;
            foreach (var entry in stats)
            {
                var stat = entry.Value;
                if (stat.Type != RTCStatsType.OutboundRtp)
                {
                    continue;
                }

                if (!GetString(stat.Dict, "kind").Equals("video"))
                {
                    continue;
                }

                count++;
                var rid = GetString(stat.Dict, "rid");
                if (string.IsNullOrEmpty(rid))
                {
                    rid = "f";
                }

                var width = GetInt(stat.Dict, "frameWidth");
                var height = GetInt(stat.Dict, "frameHeight");
                var fps = GetDouble(stat.Dict, "framesPerSecond");
                var targetBitrate = GetInt(stat.Dict, "targetBitrate");
                sb.Append(
                    $"{rid}={width}x{height} fps={fps:F1} targetBitrate={targetBitrate}; ");
            }

            if (count == 0)
            {
                sb.Append("no video outbound-rtp stats");
            }
            else
            {
                TrimTrailingSeparator(sb, "; ");
            }

            logs.Warning(sb.ToString());
        }

        public static void LogSubscriberInboundStats(ILogs logs, string context,
            IDictionary<string, RTCStats> stats)
        {
            if (stats == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append($"{Tag}[Stats][Subscriber] inbound-rtp ({context}): ");

            var count = 0;
            foreach (var entry in stats)
            {
                var stat = entry.Value;
                if (stat.Type != RTCStatsType.InboundRtp)
                {
                    continue;
                }

                if (!GetString(stat.Dict, "kind").Equals("video"))
                {
                    continue;
                }

                count++;
                var width = GetInt(stat.Dict, "frameWidth");
                var height = GetInt(stat.Dict, "frameHeight");
                var fps = GetDouble(stat.Dict, "framesPerSecond");
                sb.Append($"{width}x{height} fps={fps:F1}; ");
            }

            if (count == 0)
            {
                sb.Append("no video inbound-rtp stats");
            }
            else
            {
                TrimTrailingSeparator(sb, "; ");
            }

            logs.Warning(sb.ToString());
        }

        public static void LogSendStatsSummary(ILogs logs, string context,
            IReadOnlyList<PerformanceStats> encodeStats, IReadOnlyList<PerformanceStats> decodeStats)
        {
            var sb = new StringBuilder();
            sb.Append($"{Tag}[Stats] SendStats ({context}). encodeStats=[");
            AppendPerformanceStats(sb, encodeStats);
            sb.Append("], decodeStats=[");
            AppendPerformanceStats(sb, decodeStats);
            sb.Append(']');
            logs.Warning(sb.ToString());
        }

        private static void AppendPerformanceStats(StringBuilder sb, IReadOnlyList<PerformanceStats> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                sb.Append("none");
                return;
            }

            for (var i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                if (stat.VideoDimension != null)
                {
                    sb.Append(
                        $"{stat.TrackType}={stat.VideoDimension.Width}x{stat.VideoDimension.Height} fps={stat.AvgFps:F1} bitrate={stat.TargetBitrate}");
                }
                else
                {
                    sb.Append($"{stat.TrackType}=unknown");
                }

                if (i < stats.Count - 1)
                {
                    sb.Append(", ");
                }
            }
        }

        private static string FormatEncodings(RTCRtpSendParameters parameters)
        {
            if (parameters?.encodings == null || parameters.encodings.Length == 0)
            {
                return "none";
            }

            var sb = new StringBuilder();
            foreach (var encoding in parameters.encodings)
            {
                var rid = string.IsNullOrEmpty(encoding.rid) ? "f" : encoding.rid;
                sb.Append($"{rid}=");
                if (!encoding.active)
                {
                    sb.Append("OFF, ");
                    continue;
                }

                sb.Append("ON sc=");
                sb.Append(encoding.scaleResolutionDownBy?.ToString("F0") ?? "?");
                sb.Append(" br=");
                sb.Append(FormatBitrate(encoding.maxBitrate));
                sb.Append(", ");
            }

            TrimTrailingSeparator(sb, ", ");
            return sb.ToString();
        }

        private static string FormatResolution(VideoResolution resolution)
            => $"{resolution.Width}x{resolution.Height}";

        private static string FormatBitrate(ulong? bps)
        {
            if (!bps.HasValue)
            {
                return "?";
            }

            return bps.Value >= 1_000_000
                ? $"{bps.Value / 1_000_000d:F1}m"
                : $"{bps.Value / 1_000d:F0}k";
        }

        private static string FormatBitrate(int bps)
            => bps >= 1_000_000 ? $"{bps / 1_000_000d:F1}m" : $"{bps / 1_000d:F0}k";

        private static string GetString(IDictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var value) && value != null ? value.ToString() : string.Empty;

        private static int GetInt(IDictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out var value) || value == null)
            {
                return 0;
            }

            if (value is int i)
            {
                return i;
            }

            if (value is long l)
            {
                return (int)l;
            }

            if (value is uint ui)
            {
                return (int)ui;
            }

            if (value is double d)
            {
                return (int)d;
            }

            return int.TryParse(value.ToString(), out var result) ? result : 0;
        }

        private static double GetDouble(IDictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out var value) || value == null)
            {
                return 0.0;
            }

            if (value is double d)
            {
                return d;
            }

            if (value is float f)
            {
                return f;
            }

            if (value is int i)
            {
                return i;
            }

            if (value is long l)
            {
                return l;
            }

            return double.TryParse(value.ToString(), out var result) ? result : 0.0;
        }

        private static void TrimTrailingSeparator(StringBuilder sb, string separator)
        {
            if (sb.Length >= separator.Length && sb.ToString(sb.Length - separator.Length, separator.Length) == separator)
            {
                sb.Length -= separator.Length;
            }
        }
    }
}
#endif
