using System;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Utils;
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

            if (subscriberStatsJson == null || publisherStatsJson == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(subscriberStatsJson)}: {subscriberStatsJson}, {nameof(publisherStatsJson)}: {publisherStatsJson}");
            }

            var request = new SendStatsRequest
            {
                SessionId = _rtcSession.SessionId,
                SubscriberStats = subscriberStatsJson,
                PublisherStats = publisherStatsJson,
                WebrtcVersion = WebRTCVersion,
                Sdk = UnitySdkName,
                SdkVersion = StreamVideoLowLevelClient.SDKVersion.ToString()
            };

#if STREAM_DEBUG_ENABLED
            _logs.Info("-----------WebRTC STATS DUMP -> 1. publisher, 2. subscriber------");
            _logs.Info(publisherStatsJson);
            _logs.Info(subscriberStatsJson);
            _logs.Info("-----------END WebRTC STATS DUMP END------");
#endif

            await _rtcSession.SendWebRtcStats(request);
        }
    }
}