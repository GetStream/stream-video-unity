#if STREAM_TESTS_ENABLED
using System;
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
using StreamVideo.v1.Sfu.Models;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for the reconnection strategy selection in <see cref="RtcSession"/>.
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

        private ISfuWebSocket _sfuWebSocket;
        private INetworkMonitor _networkMonitor;
        private ITimeService _timeService;
        private TestableRtcSession _session;

        private static StreamCall CreateDummyCall()
            => new StreamCall("test:dummy",
                Substitute.For<ICacheRepository<StreamCall>>(),
                Substitute.For<IStatefulModelContext>());

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
    }
}
#endif
