#if STREAM_DEBUG_ENABLED
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Libs.Logs;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;
using TrackType = StreamVideo.Core.Models.Sfu.TrackType;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Debug-only helpers for tracing late-published remote video (subscription → subscriberOffer → render).
    /// </summary>
    internal static class LateVideoDiagnostics
    {
        private const string Tag = "[LateVideoDiag]";

        public static void LogSubscriptionDiff(
            ILogs logs,
            ref HashSet<string> previousKeys,
            IEnumerable<TrackSubscriptionDetails> tracks,
            int generation)
        {
            var newKeys = new HashSet<string>(tracks.Select(FormatSubscriptionKey));
            var added = newKeys.Except(previousKeys).ToList();
            var removed = previousKeys.Except(newKeys).ToList();
            var unchanged = added.Count == 0 && removed.Count == 0;

            logs.Warning(
                $"{Tag} UpdateSubscriptions diff gen={generation} unchanged={unchanged} " +
                $"+[{string.Join(", ", added)}] -[{string.Join(", ", removed)}] " +
                $"total={newKeys.Count}");

            previousKeys = newKeys;
        }

        public static void LogParticipantSubscribeGate(
            ILogs logs,
            IStreamVideoCallParticipant participant,
            bool subscribeAudio,
            bool subscribeVideo,
            bool incomingVideoRequested,
            bool publishingVideo)
        {
            var publishedTracks = participant is StreamVideoCallParticipant concrete
                ? string.Join(",", concrete.GetPublishedTracksDebug())
                : "?";

            logs.Warning(
                $"{Tag} Gate session={participant.SessionId} user={participant.UserId} " +
                $"published=[{publishedTracks}] incomingVideo={incomingVideoRequested} " +
                $"publishingVideo={publishingVideo} => audio={subscribeAudio} video={subscribeVideo}");
        }

        public static void LogTrackPublished(
            ILogs logs,
            string sessionId,
            TrackType trackType,
            IStreamVideoCallParticipant participant)
        {
            var publishedTracks = participant is StreamVideoCallParticipant concrete
                ? string.Join(",", concrete.GetPublishedTracksDebug())
                : "?";

            logs.Warning(
                $"{Tag} trackPublished session={sessionId} type={trackType} " +
                $"publishedTracks=[{publishedTracks}] trackPrefix={participant?.TrackLookupPrefix}");
        }

        public static void LogSubscriberOfferReceived(ILogs logs, int offerNumber, string offerSdp)
        {
            logs.Warning($"{Tag} subscriberOffer #{offerNumber} offer={SummarizeSdp(offerSdp, isOffer: true)}");
        }

        public static void LogSubscriberAnswerSent(
            ILogs logs,
            int offerNumber,
            string answerSdp,
            string subscriberConnectionState)
        {
            logs.Warning(
                $"{Tag} SendAnswer for offer #{offerNumber} answer={SummarizeSdp(answerSdp, isOffer: false)} " +
                $"subscriberPC={subscriberConnectionState}");
        }

        public static void LogSubscriberStreamAdded(
            ILogs logs,
            string trackPrefix,
            string trackTypeKey,
            IStreamVideoCallParticipant participant,
            string mediaStreamId)
        {
            logs.Warning(
                $"{Tag} SubscriberStreamAdded streamId={mediaStreamId} prefix={trackPrefix} " +
                $"type={trackTypeKey} participantSession={participant?.SessionId} " +
                $"user={participant?.UserId} prefixMatch={participant?.TrackLookupPrefix}");
        }

        public static void LogRemoteTrackAdded(
            ILogs logs,
            IStreamVideoCallParticipant participant,
            IStreamTrack track)
        {
            var extra = track is StreamVideoTrack videoTrack
                ? $"videoRotation={videoTrack.VideoRotationAngle}"
                : string.Empty;

            logs.Warning(
                $"{Tag} RemoteTrackAdded session={participant.SessionId} user={participant.UserId} " +
                $"track={track.GetType().Name} enabled={track.IsEnabled} {extra}");
        }

        public static string SummarizeSdp(string sdp, bool isOffer)
        {
            if (string.IsNullOrEmpty(sdp))
            {
                return "empty";
            }

            var originVersion = ExtractOriginSessionVersion(sdp);
            var bundle = ExtractAttributeValue(sdp, "a=group:BUNDLE");
            var mediaTypes = Regex.Matches(sdp, @"^m=(\w+)", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .ToList();
            var mids = Regex.Matches(sdp, @"^a=mid:([^\r\n]+)", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value.Trim())
                .ToList();
            var streamIds = Regex.Matches(sdp, @"a=msid:([^\s]+)")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value.Split(':').FirstOrDefault() ?? match.Groups[1].Value)
                .Distinct()
                .ToList();
            var videoCodecs = Regex.Matches(sdp, @"^a=rtpmap:\d+\s+([^/\r\n]+)", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value.Trim())
                .Where(codec => codec == "VP8" || codec == "VP9" || codec == "H264" || codec == "AV1")
                .Distinct()
                .ToList();
            var ssrcCount = Regex.Matches(sdp, @"^a=ssrc:", RegexOptions.Multiline).Count;
            var inlineCandidates = Regex.Matches(sdp, @"a=candidate:").Count;
            var firstMediaConnectionAddress = ExtractFirstMediaConnectionAddress(sdp);
            var role = isOffer ? "offer" : "answer";

            return
                $"{role} o-ver={originVersion} bundle={bundle} mids=[{string.Join(",", mids)}] " +
                $"m=[{string.Join(",", mediaTypes)}] streams=[{string.Join(",", streamIds)}] " +
                $"videoCodecs=[{string.Join(",", videoCodecs)}] ssrcCount={ssrcCount} inlineCandidates={inlineCandidates} " +
                $"firstMediaC={firstMediaConnectionAddress}";
        }

        private static string FormatSubscriptionKey(TrackSubscriptionDetails track)
        {
            var dimension = track.Dimension != null
                ? $"{track.Dimension.Width}x{track.Dimension.Height}"
                : "-";
            return $"{track.SessionId}:{track.TrackType}:{dimension}";
        }

        private static string ExtractOriginSessionVersion(string sdp)
        {
            var match = Regex.Match(sdp, @"^o=[^\r\n]+", RegexOptions.Multiline);
            if (!match.Success)
            {
                return "?";
            }

            var parts = match.Value.Split(' ');
            return parts.Length >= 3 ? parts[2] : "?";
        }

        private static string ExtractAttributeValue(string sdp, string attributePrefix)
        {
            var match = Regex.Match(sdp, $"^{Regex.Escape(attributePrefix)} (.+)$", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim() : "-";
        }

        private static string ExtractFirstMediaConnectionAddress(string sdp)
        {
            var match = Regex.Match(sdp, @"^m=\w+[^\r\n]*\r?\n(?:[^\r\n]+\r?\n)*?^c=IN IP4 ([^\r\n]+)",
                RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim() : "-";
        }
    }
}
#endif
