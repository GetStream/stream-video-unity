#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using StreamVideo.Core.Configs;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.Models;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Tests.Shared;
using StreamVideo.v1.Sfu.Events;
using StreamVideo.v1.Sfu.Models;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for subscriber-offer negotiation coordination in <see cref="RtcSession"/>.
    /// </summary>
    internal sealed class SubscriberOfferNegotiationTests
    {
        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(Substitute.For<ISfuWebSocket>());

            _session = new SubscriberOfferTestRtcSession(
                sfuWebSocketFactory: factory,
                httpClientFactory: _ => null,
                logs: Substitute.For<ILogs>(),
                serializer: Substitute.For<ISerializer>(),
                timeService: Substitute.For<ITimeService>(),
                lowLevelClient: null,
                config: StreamClientConfig.Default,
                networkMonitor: Substitute.For<INetworkMonitor>());

            _session.InitializeCancellationTokenSource();
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
        }

        [UnityTest]
        public IEnumerator When_two_offers_arrive_during_in_flight_negotiation_expect_second_waits()
            => When_two_offers_arrive_during_in_flight_negotiation_expect_second_waits_Async()
                .RunAsIEnumerator();

        private async Task When_two_offers_arrive_during_in_flight_negotiation_expect_second_waits_Async()
        {
            _session.BeginBlockingFirstNegotiation();

            InvokeSubscriberOffer(_session, CreateOffer("offer-1"));
            InvokeSubscriberOffer(_session, CreateOffer("offer-2"));

            await TestUtils.WaitUntilAsync(() => _session.NegotiateCallCount == 1,
                "The first subscriber offer should start negotiating while the second waits on the lock.");

            Assert.That(_session.MaxConcurrentNegotiations, Is.EqualTo(1),
                "Only one subscriber negotiation should run at a time.");

            _session.CompleteBlockedFirstNegotiation();

            await TestUtils.WaitUntilAsync(() => _session.NegotiateCallCount == 2,
                "The second subscriber offer should negotiate after the first completes.");

            Assert.That(_session.MaxConcurrentNegotiations, Is.EqualTo(1),
                "Concurrent negotiations must never exceed one.");
        }

        [UnityTest]
        public IEnumerator When_sfu_ice_restart_targets_subscriber_expect_subscriber_restart_ice_requested()
            => When_sfu_ice_restart_targets_subscriber_expect_subscriber_restart_ice_requested_Async()
                .RunAsIEnumerator();

        private async Task When_sfu_ice_restart_targets_subscriber_expect_subscriber_restart_ice_requested_Async()
        {
            InvokeIceRestart(_session, new ICERestart { PeerType = PeerType.Subscriber });

            await TestUtils.WaitUntilAsync(() => _session.SubscriberIceRestartRequestCount == 1,
                "Subscriber ICE restart should be requested when the SFU targets the subscriber peer.");

            Assert.That(_session.SubscriberIceRestartRequestCount, Is.EqualTo(1),
                "Exactly one subscriber ICE restart should be requested.");
        }

        private static SubscriberOffer CreateOffer(string sdp)
            => new SubscriberOffer { Sdp = sdp };

        private static void InvokeSubscriberOffer(RtcSession session, SubscriberOffer offer)
        {
            var method = typeof(RtcSession).GetMethod("OnSfuSubscriberOffer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                $"Expected private handler `OnSfuSubscriberOffer` on {nameof(RtcSession)}.");
            method.Invoke(session, new object[] { offer });
        }

        private static void InvokeIceRestart(RtcSession session, ICERestart iceRestart)
        {
            var method = typeof(RtcSession).GetMethod("OnSfuIceRestart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                $"Expected private handler `OnSfuIceRestart` on {nameof(RtcSession)}.");
            method.Invoke(session, new object[] { iceRestart });
        }

        private SubscriberOfferTestRtcSession _session;

        private sealed class SubscriberOfferTestRtcSession : RtcSession
        {
            private TaskCompletionSource<bool> _firstNegotiationBlock;
            private int _inFlightNegotiations;

            public int NegotiateCallCount { get; private set; }
            public int MaxConcurrentNegotiations { get; private set; }
            public int SubscriberIceRestartRequestCount { get; private set; }

            public SubscriberOfferTestRtcSession(ISfuWebSocketFactory sfuWebSocketFactory,
                Func<IStreamCall, HttpClient> httpClientFactory,
                ILogs logs, ISerializer serializer, ITimeService timeService,
                StreamVideoLowLevelClient lowLevelClient,
                IStreamClientConfig config, INetworkMonitor networkMonitor)
                : base(sfuWebSocketFactory, httpClientFactory, logs, serializer,
                    timeService, lowLevelClient, config, networkMonitor)
            {
            }

            public void InitializeCancellationTokenSource()
            {
                var field = typeof(RtcSession).GetField("_activeCallCts",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null,
                    $"Expected private field `_activeCallCts` on {nameof(RtcSession)} for test setup.");
                field.SetValue(this, new CancellationTokenSource());
            }

            public void BeginBlockingFirstNegotiation()
                => _firstNegotiationBlock = new TaskCompletionSource<bool>();

            public void CompleteBlockedFirstNegotiation()
                => _firstNegotiationBlock?.TrySetResult(true);

            protected override void RequestSubscriberIceRestart()
                => SubscriberIceRestartRequestCount++;

            protected override async Task NegotiateSubscriberOfferAsync(SubscriberOffer subscriberOffer,
                CancellationToken cancellationToken)
            {
                var concurrent = Interlocked.Increment(ref _inFlightNegotiations);
                try
                {
                    MaxConcurrentNegotiations = Math.Max(MaxConcurrentNegotiations, concurrent);
                    NegotiateCallCount++;

                    if (_firstNegotiationBlock != null && NegotiateCallCount == 1)
                    {
                        await _firstNegotiationBlock.Task;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _inFlightNegotiations);
                }
            }
        }
    }
}
#endif
