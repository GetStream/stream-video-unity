using System.Text;
using StreamVideo.Libs.Logs;
using Unity.WebRTC;
using SfuEvents = StreamVideo.v1.Sfu.Events;

namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// Compact debug logging for ChangePublishQuality events.
    /// Log format per layer: "rid=ON/OFF br=300k fps=15 sc=4"
    /// Abbreviations: br=bitrate, fps=framerate, sc=scaleResolutionDownBy
    /// </summary>
    internal static class PublishQualityDebugLogger
    {
        public static void LogSfuRequest(ILogs logs, string peerType,
            SfuEvents.VideoSender videoSenderSettings)
        {
            var sb = new StringBuilder();
            sb.Append($"[{peerType}] ChangePublishQuality SFU req:");

            foreach (var layer in videoSenderSettings.Layers)
            {
                sb.Append($" {layer.Name}=");
                if (!layer.Active)
                {
                    sb.Append("OFF |");
                    continue;
                }

                sb.Append("ON br=");
                sb.Append(FormatBitrate(layer.MaxBitrate));
                sb.Append(" fps=");
                sb.Append(layer.MaxFramerate);
                sb.Append(" sc=");
                sb.Append(layer.ScaleResolutionDownBy);
                sb.Append(" |");
            }

            TrimTrailingSeparator(sb);
            logs.Warning(sb.ToString());
        }

        public static void LogStateBefore(ILogs logs, string peerType,
            RTCRtpSendParameters parameters)
        {
            logs.Warning($"[{peerType}] ChangePublishQuality before: {FormatEncodings(parameters)}");
        }

        public static void LogStateAfter(ILogs logs, string peerType,
            RTCRtpSendParameters parameters)
        {
            logs.Warning($"[{peerType}] ChangePublishQuality after:  {FormatEncodings(parameters)}");
        }

        private static string FormatEncodings(RTCRtpSendParameters parameters)
        {
            var sb = new StringBuilder();
            foreach (var encoding in parameters.encodings)
            {
                var rid = string.IsNullOrEmpty(encoding.rid) ? "f" : encoding.rid;
                sb.Append($"{rid}=");
                if (!encoding.active)
                {
                    sb.Append("OFF | ");
                    continue;
                }

                sb.Append("ON br=");
                sb.Append(FormatBitrate(encoding.maxBitrate));
                sb.Append(" fps=");
                sb.Append(encoding.maxFramerate?.ToString() ?? "?");
                sb.Append(" sc=");
                sb.Append(encoding.scaleResolutionDownBy?.ToString("F0") ?? "?");
                sb.Append(" | ");
            }

            if (sb.Length >= 3)
            {
                sb.Length -= 3;
            }

            return sb.ToString();
        }

        private static string FormatBitrate(ulong? bps)
        {
            if (!bps.HasValue) return "?";
            return bps.Value >= 1_000_000
                ? $"{bps.Value / 1_000_000d:F1}m"
                : $"{bps.Value / 1_000d:F0}k";
        }

        private static string FormatBitrate(int bps)
        {
            return bps >= 1_000_000
                ? $"{bps / 1_000_000d:F1}m"
                : $"{bps / 1_000d:F0}k";
        }

        private static void TrimTrailingSeparator(StringBuilder sb)
        {
            if (sb.Length >= 2 && sb[sb.Length - 1] == '|')
            {
                sb.Length -= 2;
            }
        }
    }
}
