#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using StreamVideo.Core.Configs;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.State;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Tests.Shared;
using StreamVideo.v1.Sfu.Models;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for the retry-loop escalation logic inside <see cref="RtcSession.Reconnect"/>.
    /// Validates that the loop correctly escalates from FAST to REJOIN based on
    /// attempt count, peer connection health, fast-reconnect deadline, and error severity,
    /// and that the loop terminates on offline state or unrecoverable errors.
    /// </summary>
    internal sealed class ReconnectRetryTests
    {
        [SetUp]
        public void SetUp()
        {
            _timeService = Substitute.For<ITimeService>();
            _timeService.UtcNow.Returns(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));

            _networkMonitor = Substitute.For<INetworkMonitor>();
            _networkMonitor.IsNetworkAvailable.Returns(true);

            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(Substitute.For<ISfuWebSocket>());

            _session = new RetryTestableRtcSession(
                sfuWebSocketFactory: factory,
                httpClientFactory: _ => null,
                logs: Substitute.For<ILogs>(),
                serializer: Substitute.For<ISerializer>(),
                timeService: _timeService,
                lowLevelClient: null,
                config: Substitute.For<IStreamClientConfig>(),
                networkMonitor: _networkMonitor
            );
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
        }

        [UnityTest]
        public IEnumerator When_fast_reconnect_fails_max_allowed_times_expect_escalation_to_rejoin()
            => When_fast_reconnect_fails_max_allowed_times_expect_escalation_to_rejoin_Async().RunAsIEnumerator();

        private async Task When_fast_reconnect_fails_max_allowed_times_expect_escalation_to_rejoin_Async()
        {
            _session.CallState = CallingState.Joined;
            _session._fastReconnectDeadlineSeconds = 999;
            _session.PeerConnectionsHealthy = true;

            await _session.CallReconnect(WebsocketReconnectStrategy.Fast, "test");

            Assert.That(_session.FastReconnectCallCount,
                Is.GreaterThanOrEqualTo(RtcSession.CallRejoinMaxFastAttempts),
                "FAST should be attempted at least CallRejoinMaxFastAttempts times before escalating.");
            Assert.That(_session.FastReconnectCallCount,
                Is.LessThanOrEqualTo(RtcSession.CallRejoinMaxFastAttempts + 1),
                "FAST attempts should be bounded by CallRejoinMaxFastAttempts.");
            Assert.That(_session.RejoinCallCount, Is.EqualTo(1),
                "After exhausting FAST attempts, exactly one REJOIN should be triggered to complete the reconnection.");
        }

        [UnityTest]
        public IEnumerator When_fast_reconnect_fails_and_peer_connections_unhealthy_expect_immediate_rejoin()
            => When_fast_reconnect_fails_and_peer_connections_unhealthy_expect_immediate_rejoin_Async().RunAsIEnumerator();

        private async Task When_fast_reconnect_fails_and_peer_connections_unhealthy_expect_immediate_rejoin_Async()
        {
            _session.CallState = CallingState.Joined;
            _session._fastReconnectDeadlineSeconds = 999;
            _session.PeerConnectionsHealthy = true;
            _session.OnFastReconnectCalled = session => session.PeerConnectionsHealthy = false;

            await _session.CallReconnect(WebsocketReconnectStrategy.Fast, "test");

            Assert.That(_session.FastReconnectCallCount, Is.EqualTo(1),
                "FAST should be attempted exactly once; when peer connections become unhealthy " +
                "during the attempt, there is no point retrying FAST.");
            Assert.That(_session.RejoinCallCount, Is.EqualTo(1),
                "Unhealthy peer connections should cause immediate escalation to REJOIN " +
                "without waiting for CallRejoinMaxFastAttempts.");
        }

        [UnityTest]
        public IEnumerator When_fast_reconnect_deadline_exceeded_expect_escalation_to_rejoin()
            => When_fast_reconnect_deadline_exceeded_expect_escalation_to_rejoin_Async().RunAsIEnumerator();

        private async Task When_fast_reconnect_deadline_exceeded_expect_escalation_to_rejoin_Async()
        {
            _session.CallState = CallingState.Joined;
            _session._fastReconnectDeadlineSeconds = 30;
            _session.PeerConnectionsHealthy = true;

            var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _timeService.UtcNow.Returns(baseTime, baseTime.AddSeconds(31));

            await _session.CallReconnect(WebsocketReconnectStrategy.Fast, "test");

            Assert.That(_session.FastReconnectCallCount, Is.EqualTo(1),
                "FAST should be attempted exactly once; after the first failure the elapsed time " +
                "exceeds the 30-second deadline, triggering immediate escalation.");
            Assert.That(_session.RejoinCallCount, Is.EqualTo(1),
                "Exceeding the fast reconnect deadline should escalate to REJOIN " +
                "because the SFU session has likely expired.");
        }

        [UnityTest]
        public IEnumerator When_device_goes_offline_during_reconnect_expect_loop_stops()
            => When_device_goes_offline_during_reconnect_expect_loop_stops_Async().RunAsIEnumerator();

        private async Task When_device_goes_offline_during_reconnect_expect_loop_stops_Async()
        {
            _session.CallState = CallingState.Joined;
            _session._fastReconnectDeadlineSeconds = 999;
            _session.OnFastReconnectCalled = _ => _networkMonitor.IsNetworkAvailable.Returns(false);

            await _session.CallReconnect(WebsocketReconnectStrategy.Fast, "test");

            Assert.That(_session.FastReconnectCallCount, Is.EqualTo(1),
                "Reconnection should stop after detecting network is down without additional retry attempts.");
            Assert.That(_session.RejoinCallCount, Is.EqualTo(0),
                "Going offline should stop the reconnection loop entirely, not escalate to REJOIN.");
        }

        [UnityTest]
        public IEnumerator When_migrate_reconnect_fails_expect_immediate_escalation_to_rejoin()
            => When_migrate_reconnect_fails_expect_immediate_escalation_to_rejoin_Async().RunAsIEnumerator();

        private async Task When_migrate_reconnect_fails_expect_immediate_escalation_to_rejoin_Async()
        {
            _session.CallState = CallingState.Joined;
            _session._fastReconnectDeadlineSeconds = 999;
            _session.PeerConnectionsHealthy = true;

            await _session.CallReconnect(WebsocketReconnectStrategy.Migrate, "test");

            Assert.That(_session.FastReconnectCallCount, Is.EqualTo(0),
                "Migration failure should escalate directly to REJOIN, never to FAST.");
            Assert.That(_session.RejoinCallCount, Is.EqualTo(1),
                "When Migrate reconnect fails (NotImplementedException), wasMigrating=true " +
                "should cause immediate escalation to REJOIN.");
        }

        [UnityTest]
        public IEnumerator When_unrecoverable_api_error_during_reconnect_expect_reconnecting_failed()
            => When_unrecoverable_api_error_during_reconnect_expect_reconnecting_failed_Async().RunAsIEnumerator();

        private async Task When_unrecoverable_api_error_during_reconnect_expect_reconnecting_failed_Async()
        {
            _session.CallState = CallingState.Joined;
            _session._fastReconnectDeadlineSeconds = 999;
            _session.FastReconnectException = new StreamApiException(
                new APIErrorInternalDTO { Unrecoverable = true, Message = "Unrecoverable test error" });

            try
            {
                await _session.CallReconnect(WebsocketReconnectStrategy.Fast, "test");
                Assert.Fail("Expected StreamApiException to propagate from Reconnect.");
            }
            catch (StreamApiException)
            {
            }

            Assert.That(_session.CallState, Is.EqualTo(CallingState.ReconnectingFailed),
                "An unrecoverable API error during reconnection should set CallState to ReconnectingFailed.");
            Assert.That(_session.FastReconnectCallCount, Is.EqualTo(1),
                "Reconnection should stop after the first unrecoverable error without retrying.");
            Assert.That(_session.RejoinCallCount, Is.EqualTo(0),
                "Unrecoverable errors should not escalate to REJOIN — there is no point retrying.");
        }

        private ITimeService _timeService;
        private INetworkMonitor _networkMonitor;
        private RetryTestableRtcSession _session;

        /// <summary>
        /// Test subclass of <see cref="RtcSession"/> that lets the real
        /// <see cref="RtcSession.Reconnect"/> retry loop run, but stubs out the
        /// actual FAST / REJOIN operations and the inter-retry delay. This allows
        /// tests to verify the escalation logic (attempt counting, deadline, PC health)
        /// in isolation without real networking, WebRTC, or wall-clock delays.
        /// </summary>
        private class RetryTestableRtcSession : RtcSession
        {
            public bool PeerConnectionsHealthy { get; set; } = true;
            public int FastReconnectCallCount { get; private set; }
            public int RejoinCallCount { get; private set; }
            public int MigrateCallCount { get; private set; }
            public Action<RetryTestableRtcSession> OnFastReconnectCalled { get; set; }
            public Exception FastReconnectException { get; set; } = new Exception("Simulated FAST reconnect failure");

            public RetryTestableRtcSession(ISfuWebSocketFactory sfuWebSocketFactory,
                Func<IStreamCall, HttpClient> httpClientFactory,
                ILogs logs, ISerializer serializer, ITimeService timeService,
                StreamVideoLowLevelClient lowLevelClient,
                IStreamClientConfig config, INetworkMonitor networkMonitor)
                : base(sfuWebSocketFactory, httpClientFactory, logs, serializer,
                    timeService, lowLevelClient, config, networkMonitor)
            {
            }

            public Task CallReconnect(WebsocketReconnectStrategy strategy, string reason)
                => Reconnect(strategy, reason);

            protected override bool ArePeerConnectionsHealthy() => PeerConnectionsHealthy;

            protected override Task ReconnectRetryDelay() => Task.CompletedTask;

            protected override Task ReconnectFast()
            {
                FastReconnectCallCount++;
                CallState = CallingState.Reconnecting;
                OnFastReconnectCalled?.Invoke(this);
                throw FastReconnectException;
            }

            protected override Task ReconnectRejoin()
            {
                RejoinCallCount++;
                CallState = CallingState.Joined;
                return Task.CompletedTask;
            }

            protected override Task ReconnectMigrate()
            {
                MigrateCallCount++;
                CallState = CallingState.Migrating;
                throw new NotImplementedException("Simulated Migrate failure");
            }
        }
    }
}
#endif
