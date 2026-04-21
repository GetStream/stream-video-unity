using System;
using System.Threading;
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

            if (_rtcSession.CallState != CallingState.Joined)
            {
                return;
            }

            if (_timeService.Time > _lastTimeSent + SendInterval && _currentSendTask == null)
            {
                _periodicStatsCts = new CancellationTokenSource();
                var cts = _periodicStatsCts;
                _currentSendTask = CollectAndSend(cts.Token).ContinueWith(t =>
                {
                    _currentSendTask = null;
                    cts.Dispose();
                    if (ReferenceEquals(_periodicStatsCts, cts))
                    {
                        _periodicStatsCts = null;
                    }

                    if (_rtcSession.CallState == CallingState.Joining)
                    {
                        t.LogIfFailed();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
                _lastTimeSent = _timeService.Time;
            }
        }

        /// <summary>
        /// Sends final stats immediately, flushing any remaining trace data.
        /// Called when leaving a call to ensure all stats are captured.
        /// </summary>
        public async Task SendFinalStatsAsync(CancellationToken cancellationToken)
        {
            if (_rtcSession.ActiveCall == null)
            {
                return;
            }

            if (_currentSendTask != null)
            {
                var maxWaitForCurrentSend = Task.Delay(500, cancellationToken);
                var completedFirst = await Task.WhenAny(_currentSendTask, maxWaitForCurrentSend);
                if (completedFirst != _currentSendTask)
                {
                    _periodicStatsCts?.Cancel();
                    await Task.WhenAny(_currentSendTask, Task.Delay(Timeout.Infinite, cancellationToken));
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            await CollectAndSend(cancellationToken);
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
        private CancellationTokenSource _periodicStatsCts;

        private async Task CollectAndSend(CancellationToken cancellationToken)
        {
            if (_rtcSession.ActiveCall == null)
            {
                return;
            }
            
            if(_rtcSession.Publisher == null || _rtcSession.Subscriber == null)
            {
                _logs.Warning("WebRtcStatsSender: Publisher or Subscriber is null, skipping stats collection.");
                return;
            }
            
            var stats = await _webRtcStatsCollector.CollectAsync(cancellationToken);

            if (stats.SubscriberStatsJson == null || stats.PublisherStatsJson == null || stats.RtcStatsJson == null)
            {
                throw new InvalidOperationException(
                    $"SubscriberStatsJson: {stats.SubscriberStatsJson}, PublisherStatsJson: {stats.PublisherStatsJson}, RtcStatsJson: {stats.RtcStatsJson}");
            }

            var request = new SendStatsRequest
            {
                SessionId = _rtcSession.SessionId.ToString(),
                UnifiedSessionId = _rtcSession.ActiveCall.UnifiedSessionId,
                SubscriberStats = stats.SubscriberStatsJson,
                PublisherStats = stats.PublisherStatsJson,
                RtcStats = stats.RtcStatsJson,
                WebrtcVersion = WebRTCVersion,
                Sdk = UnitySdkName,
                SdkVersion = StreamVideoLowLevelClient.SDKVersion.ToString(),
            };

            foreach (var stat in stats.EncodeStats)
            {
                request.EncodeStats.Add(stat);
            }
            
            foreach (var stat in stats.DecodeStats)
            {
                request.DecodeStats.Add(stat);
            }

#pragma warning disable CS0162 // Disable unreachable code warning
#if STREAM_DEBUG_ENABLED
            if (RtcSession.LogWebRTCStats)
            {
                _logs.Info("-----------WebRTC STATS DUMP -> 1. publisher, 2. subscriber, 3. rtc_stats------");
                _logs.Info(stats.PublisherStatsJson);
                _logs.Info(stats.SubscriberStatsJson);
                _logs.Info(stats.RtcStatsJson);
                _logs.Info($"Encode stats count: {stats.EncodeStats.Count}, Decode stats count: {stats.DecodeStats.Count}");
                _logs.Info(request.ToString());
                _logs.Info("-----------END WebRTC STATS DUMP END------");
            }
#endif
#pragma warning restore CS0162 // Re-enable unreachable code warning

            cancellationToken.ThrowIfCancellationRequested();
            await _rtcSession.SendWebRtcStats(request, cancellationToken);
        }
    }
}