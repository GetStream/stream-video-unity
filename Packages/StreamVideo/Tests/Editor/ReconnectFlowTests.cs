#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using StreamVideo.Core.Configs;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
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
    /// Tests for the reconnection flow in <see cref="RtcSession"/>.
    /// Validates that <see cref="RtcSession"/> chooses the correct
    /// <see cref="WebsocketReconnectStrategy"/> in response to SFU WebSocket
    /// and network availability events, based on peer connection health,
    /// calling state, and offline duration.
    /// </summary>
    internal sealed class ReconnectFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            _sfuWebSocket = Substitute.For<ISfuWebSocket>();
            _sfuWebSocket.IsLeaving.Returns(false);
            _sfuWebSocket.IsClosingClean.Returns(false);
            _sfuWebSocket.DisconnectAsync(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string>())
                .Returns(Task.CompletedTask);

            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(_sfuWebSocket);

            _networkMonitor = Substitute.For<INetworkMonitor>();
            _timeService = Substitute.For<ITimeService>();

            _session = new TestableRtcSession(
                sfuWebSocketFactory: factory,
                httpClientFactory: _ => null,
                logs: Substitute.For<ILogs>(),
                serializer: Substitute.For<ISerializer>(),
                timeService: _timeService,
                lowLevelClient: null,
                config: Substitute.For<IStreamClientConfig>(),
                networkMonitor: _networkMonitor
            );

            _session.CreateNewSfuWebSocket(out _);
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
        }

        [Test]
        public void When_sfu_websocket_disconnects_and_peer_connections_healthy_expect_fast_reconnect()
        {
            _session.CallState = CallingState.Joined;
            _session.PeerConnectionsHealthy = true;

            _sfuWebSocket.Disconnected += Raise.Event<Action>();

            Assert.That(_session.LastReconnectStrategy, Is.EqualTo(WebsocketReconnectStrategy.Fast),
                "When both peer connections are healthy, the SFU WS disconnect should trigger " +
                "a Fast reconnect that reuses existing peer connections and establishes a new WebSocket only.");
        }

        [Test]
        public void When_sfu_websocket_disconnects_and_peer_connections_unhealthy_expect_rejoin()
        {
            _session.CallState = CallingState.Joined;
            _session.PeerConnectionsHealthy = false;

            _sfuWebSocket.Disconnected += Raise.Event<Action>();

            Assert.That(_session.LastReconnectStrategy, Is.EqualTo(WebsocketReconnectStrategy.Rejoin),
                "When peer connections are unhealthy, the SFU WS disconnect should trigger " +
                "a Rejoin that creates new peer connections and a new WebSocket.");
        }

        [Test]
        public void When_network_comes_back_online_within_fast_deadline_expect_fast_reconnect()
        {
            _session.CallState = CallingState.Joined;
            _session.ActiveCall = CreateDummyCall();
            _session._fastReconnectDeadlineSeconds = 30;

            var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            _timeService.UtcNow.Returns(baseTime);
            _networkMonitor.NetworkAvailabilityChanged
                += Raise.Event<NetworkAvailabilityChangedEventHandler>(false);

            _timeService.UtcNow.Returns(baseTime.AddSeconds(10));
            _networkMonitor.NetworkAvailabilityChanged
                += Raise.Event<NetworkAvailabilityChangedEventHandler>(true);

            Assert.That(_session.LastReconnectStrategy, Is.EqualTo(WebsocketReconnectStrategy.Fast),
                "When the device was offline for 10 seconds with a 30-second deadline, " +
                "a Fast reconnect should be used because the SFU session is still alive.");
        }

        [Test]
        public void When_network_comes_back_online_after_fast_deadline_expect_rejoin()
        {
            _session.CallState = CallingState.Joined;
            _session.ActiveCall = CreateDummyCall();
            _session._fastReconnectDeadlineSeconds = 30;

            var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            _timeService.UtcNow.Returns(baseTime);
            _networkMonitor.NetworkAvailabilityChanged
                += Raise.Event<NetworkAvailabilityChangedEventHandler>(false);

            _timeService.UtcNow.Returns(baseTime.AddSeconds(60));
            _networkMonitor.NetworkAvailabilityChanged
                += Raise.Event<NetworkAvailabilityChangedEventHandler>(true);

            Assert.That(_session.LastReconnectStrategy, Is.EqualTo(WebsocketReconnectStrategy.Rejoin),
                "When the device was offline for 60 seconds with a 30-second deadline, " +
                "a Rejoin is required because the SFU will have already cleaned up the session.");
        }

        [Test]
        public void When_network_goes_offline_expect_state_transitions_to_offline()
        {
            _session.CallState = CallingState.Joined;
            _session.ActiveCall = CreateDummyCall();
            _timeService.UtcNow.Returns(DateTime.UtcNow);

            _networkMonitor.NetworkAvailabilityChanged
                += Raise.Event<NetworkAvailabilityChangedEventHandler>(false);

            Assert.That(_session.CallState, Is.EqualTo(CallingState.Offline),
                "Losing network connectivity should immediately transition " +
                "the call state to Offline when there is an active call.");
        }

        [UnityTest]
        public IEnumerator When_fast_reconnect_fails_max_allowed_times_expect_escalation_to_rejoin()
            => When_fast_reconnect_fails_max_allowed_times_expect_escalation_to_rejoin_Async().RunAsIEnumerator();

        private async Task When_fast_reconnect_fails_max_allowed_times_expect_escalation_to_rejoin_Async()
        {
            using var retrySession = CreateRetrySession();
            retrySession.CallState = CallingState.Joined;
            retrySession._fastReconnectDeadlineSeconds = 999;
            retrySession.PeerConnectionsHealthy = true;

            await retrySession.CallReconnect(WebsocketReconnectStrategy.Fast, "test");

            Assert.That(retrySession.FastReconnectCallCount, Is.GreaterThan(1),
                "FAST should be retried more than once before giving up.");
            Assert.That(retrySession.FastReconnectCallCount,
                Is.LessThanOrEqualTo(RtcSession.CallRejoinMaxFastAttempts + 1),
                "FAST attempts should be bounded by CallRejoinMaxFastAttempts.");
            Assert.That(retrySession.RejoinCallCount, Is.EqualTo(1),
                "After exhausting FAST attempts, exactly one REJOIN should be triggered to complete the reconnection.");
        }

        [UnityTest]
        public IEnumerator When_fast_reconnect_fails_and_peer_connections_unhealthy_expect_immediate_rejoin()
            => When_fast_reconnect_fails_and_peer_connections_unhealthy_expect_immediate_rejoin_Async().RunAsIEnumerator();

        private async Task When_fast_reconnect_fails_and_peer_connections_unhealthy_expect_immediate_rejoin_Async()
        {
            using var retrySession = CreateRetrySession();
            retrySession.CallState = CallingState.Joined;
            retrySession._fastReconnectDeadlineSeconds = 999;
            retrySession.PeerConnectionsHealthy = true;
            retrySession.OnFastReconnectCalled = session => session.PeerConnectionsHealthy = false;

            await retrySession.CallReconnect(WebsocketReconnectStrategy.Fast, "test");

            Assert.That(retrySession.FastReconnectCallCount, Is.EqualTo(1),
                "FAST should be attempted exactly once; when peer connections become unhealthy " +
                "during the attempt, there is no point retrying FAST.");
            Assert.That(retrySession.RejoinCallCount, Is.EqualTo(1),
                "Unhealthy peer connections should cause immediate escalation to REJOIN " +
                "without waiting for CallRejoinMaxFastAttempts.");
        }

        [UnityTest]
        public IEnumerator When_fast_reconnect_deadline_exceeded_expect_escalation_to_rejoin()
            => When_fast_reconnect_deadline_exceeded_expect_escalation_to_rejoin_Async().RunAsIEnumerator();

        private async Task When_fast_reconnect_deadline_exceeded_expect_escalation_to_rejoin_Async()
        {
            using var retrySession = CreateRetrySession();
            retrySession.CallState = CallingState.Joined;
            retrySession._fastReconnectDeadlineSeconds = 0;
            retrySession.PeerConnectionsHealthy = true;

            await retrySession.CallReconnect(WebsocketReconnectStrategy.Fast, "test");

            Assert.That(retrySession.FastReconnectCallCount, Is.EqualTo(1),
                "FAST should be attempted exactly once; after the inter-retry delay the elapsed time " +
                "exceeds the zero-second deadline, triggering immediate escalation.");
            Assert.That(retrySession.RejoinCallCount, Is.EqualTo(1),
                "Exceeding the fast reconnect deadline should escalate to REJOIN " +
                "because the SFU session has likely expired.");
        }

        private ISfuWebSocket _sfuWebSocket;
        private INetworkMonitor _networkMonitor;
        private ITimeService _timeService;
        private TestableRtcSession _session;

        private static StreamCall CreateDummyCall()
            => new StreamCall("test:dummy",
                Substitute.For<ICacheRepository<StreamCall>>(),
                Substitute.For<IStatefulModelContext>());

        private RetryTestableRtcSession CreateRetrySession()
        {
            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(Substitute.For<ISfuWebSocket>());

            var session = new RetryTestableRtcSession(
                sfuWebSocketFactory: factory,
                httpClientFactory: _ => null,
                logs: Substitute.For<ILogs>(),
                serializer: Substitute.For<ISerializer>(),
                timeService: Substitute.For<ITimeService>(),
                lowLevelClient: null,
                config: Substitute.For<IStreamClientConfig>(),
                networkMonitor: Substitute.For<INetworkMonitor>()
            );

            return session;
        }

        /// <summary>
        /// Test subclass of <see cref="RtcSession"/> that overrides peer connection
        /// health and reconnect execution, allowing tests to control inputs and verify
        /// the chosen strategy without real networking or WebRTC dependencies.
        /// </summary>
        private class TestableRtcSession : RtcSession
        {
            public bool PeerConnectionsHealthy { get; set; }
            public WebsocketReconnectStrategy? LastReconnectStrategy { get; private set; }
            public string LastReconnectReason { get; private set; }

            public TestableRtcSession(ISfuWebSocketFactory sfuWebSocketFactory,
                Func<IStreamCall, HttpClient> httpClientFactory,
                ILogs logs, ISerializer serializer, ITimeService timeService,
                StreamVideoLowLevelClient lowLevelClient,
                IStreamClientConfig config, INetworkMonitor networkMonitor)
                : base(sfuWebSocketFactory, httpClientFactory, logs, serializer,
                    timeService, lowLevelClient, config, networkMonitor)
            {
            }

            protected override bool ArePeerConnectionsHealthy() => PeerConnectionsHealthy;

            protected override Task Reconnect(WebsocketReconnectStrategy strategy, string reason)
            {
                LastReconnectStrategy = strategy;
                LastReconnectReason = reason;
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Test subclass of <see cref="RtcSession"/> that lets the real
        /// <see cref="RtcSession.Reconnect"/> retry loop run, but stubs out the
        /// actual FAST and REJOIN operations. This allows tests to verify the
        /// escalation logic (attempt counting, deadline, PC health) in isolation.
        /// </summary>
        private class RetryTestableRtcSession : RtcSession
        {
            public bool PeerConnectionsHealthy { get; set; } = true;
            public int FastReconnectCallCount { get; private set; }
            public int RejoinCallCount { get; private set; }
            public Action<RetryTestableRtcSession> OnFastReconnectCalled { get; set; }

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

            protected override Task ReconnectFast()
            {
                FastReconnectCallCount++;
                CallState = CallingState.Reconnecting;
                OnFastReconnectCalled?.Invoke(this);
                throw new Exception("Simulated FAST reconnect failure");
            }

            protected override Task ReconnectRejoin()
            {
                RejoinCallCount++;
                CallState = CallingState.Joined;
                return Task.CompletedTask;
            }
        }
    }
}
#endif
