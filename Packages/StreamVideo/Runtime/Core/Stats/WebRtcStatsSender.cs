using System;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Utils;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;

namespace StreamVideo.Core.Stats
{
    internal class WebRtcStatsSender
    {
        public void Update()
        {
            if (_rtcSession.ActiveCall == null)
            {
                return;
            }

            if (_timeService.Time > _lastTimeSent + SendInterval && _currentSendTask == null)
            {
                _currentSendTask = CollectAndSend().ContinueWith(t =>
                {
                    _currentSendTask = null;
                    t.LogIfFailed();
                });
                _lastTimeSent = _timeService.Time;
            }
        }

        /// <summary>
        /// Sends final stats immediately, flushing any remaining trace data.
        /// Called when leaving a call to ensure all stats are captured.
        /// </summary>
        public async Task SendFinalStatsAsync()
        {
            if (_rtcSession.ActiveCall == null)
            {
                return;
            }

            if (_currentSendTask != null)
            {
                await _currentSendTask;
            }

            await CollectAndSend();
        }

        internal WebRtcStatsSender(RtcSession rtcSession, IWebRtcStatsCollector webRtcStatsCollector,
            ITimeService timeService, ILogs logs)
        {
            _rtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            _webRtcStatsCollector
                = webRtcStatsCollector ?? throw new ArgumentNullException(nameof(webRtcStatsCollector));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        }

        private const int SendInterval = 10;

        private const string UnitySdkName = "stream-unity";

        //StreamTodo: hardcoded webRTC version but currently Unity's webRTC doesn't expose version number. We could extract it from the internal C++ webRTC code + add test to verify
        private const string WebRTCVersion = "M116";

        private readonly RtcSession _rtcSession;
        private readonly ITimeService _timeService;
        private readonly IWebRtcStatsCollector _webRtcStatsCollector;
        private readonly ILogs _logs;

        private float _lastTimeSent;
        private Task _currentSendTask;

        private async Task CollectAndSend()
        {
            var subscriberStatsJson = await _webRtcStatsCollector.GetSubscriberStatsJsonAsync();
            var publisherStatsJson = await _webRtcStatsCollector.GetPublisherStatsJsonAsync();
            var rtcStatsJson = await _webRtcStatsCollector.GetRtcStatsJsonAsync();
            var encodeStats = await _webRtcStatsCollector.GetEncodeStatsAsync();
            var decodeStats = await _webRtcStatsCollector.GetDecodeStatsAsync();

            if (subscriberStatsJson == null || publisherStatsJson == null || rtcStatsJson == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(subscriberStatsJson)}: {subscriberStatsJson}, {nameof(publisherStatsJson)}: {publisherStatsJson}, {nameof(rtcStatsJson)}: {rtcStatsJson}");
            }

            var request = new SendStatsRequest
            {
                SessionId = _rtcSession.SessionId,
                UnifiedSessionId = _rtcSession.ActiveCall.UnifiedSessionId,
                SubscriberStats = subscriberStatsJson,
                PublisherStats = publisherStatsJson,
                RtcStats = rtcStatsJson,
                WebrtcVersion = WebRTCVersion,
                Sdk = UnitySdkName,
                SdkVersion = StreamVideoLowLevelClient.SDKVersion.ToString(),
            };

            foreach (var stat in encodeStats)
            {
                request.EncodeStats.Add(stat);
            }
            
            foreach (var stat in decodeStats)
            {
                request.DecodeStats.Add(stat);
            }

#pragma warning disable CS0162 // Disable unreachable code warning
#if STREAM_DEBUG_ENABLED
            if (RtcSession.LogWebRTCStats)
            {
                _logs.Info("-----------WebRTC STATS DUMP -> 1. publisher, 2. subscriber, 3. rtc_stats------");
                _logs.Info(publisherStatsJson);
                _logs.Info(subscriberStatsJson);
                _logs.Info(rtcStatsJson);
                _logs.Info($"Encode stats count: {encodeStats.Count}, Decode stats count: {decodeStats.Count}");
                _logs.Info(request.ToString());
                _logs.Info("-----------END WebRTC STATS DUMP END------");
            }
#endif
#pragma warning restore CS0162 // Re-enable unreachable code warning

            await _rtcSession.SendWebRtcStats(request);
        }
    }
}