#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Tests.Shared;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for track subscription request coordination in <see cref="RtcSession"/>.
    /// </summary>
    internal sealed class TrackSubscriptionRequestRaceTests
    {
        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
            _timeService = Substitute.For<ITimeService>();
            _timeService.Time.Returns(_ => _currentTime);

            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(Substitute.For<ISfuWebSocket>());

            _session = new TrackSubscriptionTestRtcSession(
                sfuWebSocketFactory: factory,
                httpClientFactory: _ => null,
                logs: Substitute.For<ILogs>(),
                serializer: Substitute.For<ISerializer>(),
                timeService: _timeService,
                lowLevelClient: null,
                config: StreamClientConfig.Default,
                networkMonitor: Substitute.For<INetworkMonitor>());
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
        }

        [UnityTest]
        public IEnumerator When_request_queued_during_in_flight_update_subscriptions_expect_follow_up_rpc()
            => When_request_queued_during_in_flight_update_subscriptions_expect_follow_up_rpc_Async()
                .RunAsIEnumerator();

        private async Task When_request_queued_during_in_flight_update_subscriptions_expect_follow_up_rpc_Async()
        {
            SetupCallWithRemoteParticipant("remote-session");
            _session.UpdateIncomingAudioRequested("remote-session", true);

            _session.BeginBlockingFirstUpdateSubscriptionsRpc();
            AdvanceTimePastDebounce();
            _session.Update();

            await TestUtils.WaitUntilAsync(() => _session.UpdateSubscriptionsCallCount == 1,
                "The first UpdateSubscriptions RPC should start while the request is in flight.");

            _session.UpdateIncomingVideoRequested("remote-session", true);
            _session.CompleteBlockedFirstUpdateSubscriptionsRpc();

            await TestUtils.WaitUntilAsync(() => !_session.IsTrackSubscriptionRequestInProgress,
                "The in-flight flag should clear after the first RPC completes.");

            Assert.That(_session.IsTrackSubscriptionRequested, Is.True,
                "A subscription change queued during an in-flight RPC must remain pending for a follow-up RPC.");

            AdvanceTimePastDebounce();
            _session.Update();

            await TestUtils.WaitUntilAsync(() => _session.UpdateSubscriptionsCallCount == 2,
                "A follow-up UpdateSubscriptions RPC should be sent after the stale in-flight request completes.");
        }

        [UnityTest]
        public IEnumerator When_update_subscriptions_rpc_throws_expect_flag_cleared_and_request_retained()
            => When_update_subscriptions_rpc_throws_expect_flag_cleared_and_request_retained_Async()
                .RunAsIEnumerator(ignoreFailingMessages: true);

        private async Task When_update_subscriptions_rpc_throws_expect_flag_cleared_and_request_retained_Async()
        {
            SetupCallWithRemoteParticipant("remote-session");
            _session.UpdateIncomingAudioRequested("remote-session", true);
            _session.FailNextUpdateSubscriptionsRpc = true;

            AdvanceTimePastDebounce();
            _session.Update();

            await TestUtils.WaitUntilAsync(() => _session.UpdateSubscriptionsCallCount == 1,
                "The failing UpdateSubscriptions RPC should be attempted.");

            await TestUtils.WaitUntilAsync(() => !_session.IsTrackSubscriptionRequestInProgress,
                "The in-flight flag must be cleared even when the RPC throws.");

            Assert.That(_session.IsTrackSubscriptionRequested, Is.True,
                "A failed RPC must retain the pending subscription request for retry.");

            AdvanceTimePastDebounce();
            _session.Update();

            await TestUtils.WaitUntilAsync(() => _session.UpdateSubscriptionsCallCount == 2,
                "The pending subscription request should be retried after the failed RPC.");
        }

        private void SetupCallWithRemoteParticipant(string remoteSessionId)
        {
            _session.ActiveCall = CreateCallWithRemoteParticipant(_session, remoteSessionId);
        }

        private void AdvanceTimePastDebounce()
        {
            _currentTime += TrackSubscriptionDebounceTime + 0.01f;
        }

        private static StreamCall CreateCallWithRemoteParticipant(RtcSession session, string remoteSessionId)
        {
            var call = new StreamCall("test:call",
                Substitute.For<ICacheRepository<StreamCall>>(),
                Substitute.For<IStatefulModelContext>());

            var participantContext = CreateParticipantContext(session);
            var participant = new StreamVideoCallParticipant(
                remoteSessionId,
                Substitute.For<ICacheRepository<StreamVideoCallParticipant>>(),
                participantContext);

            ((IUpdateableFrom<Participant, StreamVideoCallParticipant>)participant).UpdateFromDto(
                new Participant { SessionId = remoteSessionId, UserId = "remote-user" },
                participantContext.Cache);

            var callSession = new CallSession();
            var participants = (List<StreamVideoCallParticipant>)typeof(CallSession)
                .GetField("_participants", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(callSession);
            participants.Add(participant);

            typeof(StreamCall)
                .GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(call, callSession);

            return call;
        }

        private static IStatefulModelContext CreateParticipantContext(RtcSession session)
        {
            var context = Substitute.For<IStatefulModelContext>();
            context.Cache.Returns(Substitute.For<ICache>());
            context.Logs.Returns(Substitute.For<ILogs>());
            context.Serializer.Returns(Substitute.For<ISerializer>());

            var client = Substitute.For<IInternalStreamVideoClient>();
            client.InternalLowLevelClient.Returns(CreateLowLevelClientShim(session));
            context.Client.Returns(client);

            return context;
        }

        private static StreamVideoLowLevelClient CreateLowLevelClientShim(RtcSession session)
        {
            var lowLevelClient = (StreamVideoLowLevelClient)RuntimeHelpers.GetUninitializedObject(
                typeof(StreamVideoLowLevelClient));

            foreach (var field in typeof(StreamVideoLowLevelClient).GetFields(
                         BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(RtcSession))
                {
                    field.SetValue(lowLevelClient, session);
                }
            }

            return lowLevelClient;
        }

        private float _currentTime;
        private ITimeService _timeService;
        private TrackSubscriptionTestRtcSession _session;

        private const float TrackSubscriptionDebounceTime = 0.1f;

        private sealed class TrackSubscriptionTestRtcSession : RtcSession
        {
            private TaskCompletionSource<bool> _firstRpcBlock;

            public int UpdateSubscriptionsCallCount { get; private set; }
            public bool FailNextUpdateSubscriptionsRpc { get; set; }

            public bool IsTrackSubscriptionRequestInProgress =>
                GetTrackSubscriptionField<bool>("_trackSubscriptionRequestInProgress");

            public bool IsTrackSubscriptionRequested =>
                GetTrackSubscriptionField<bool>("_trackSubscriptionRequested");

            public TrackSubscriptionTestRtcSession(ISfuWebSocketFactory sfuWebSocketFactory,
                Func<IStreamCall, HttpClient> httpClientFactory,
                ILogs logs, ISerializer serializer, ITimeService timeService,
                StreamVideoLowLevelClient lowLevelClient,
                IStreamClientConfig config, INetworkMonitor networkMonitor)
                : base(sfuWebSocketFactory, httpClientFactory, logs, serializer,
                    timeService, lowLevelClient, config, networkMonitor)
            {
            }

            public void BeginBlockingFirstUpdateSubscriptionsRpc()
                => _firstRpcBlock = new TaskCompletionSource<bool>();

            public void CompleteBlockedFirstUpdateSubscriptionsRpc()
                => _firstRpcBlock?.TrySetResult(true);

            protected override Task<UpdateSubscriptionsResponse> SendUpdateSubscriptionsAsync(
                UpdateSubscriptionsRequest request, CancellationToken cancellationToken)
            {
                UpdateSubscriptionsCallCount++;

                if (FailNextUpdateSubscriptionsRpc)
                {
                    FailNextUpdateSubscriptionsRpc = false;
                    return Task.FromException<UpdateSubscriptionsResponse>(
                        new InvalidOperationException("Simulated UpdateSubscriptions RPC failure"));
                }

                if (_firstRpcBlock != null && UpdateSubscriptionsCallCount == 1)
                {
                    return _firstRpcBlock.Task.ContinueWith(_ => new UpdateSubscriptionsResponse());
                }

                return Task.FromResult(new UpdateSubscriptionsResponse());
            }

            private static T GetTrackSubscriptionField<T>(RtcSession session, string fieldName)
            {
                var field = typeof(RtcSession).GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null,
                    $"Expected private field `{fieldName}` on {nameof(RtcSession)} for test inspection.");
                return (T)field.GetValue(session);
            }

            private T GetTrackSubscriptionField<T>(string fieldName)
                => GetTrackSubscriptionField<T>(this, fieldName);
        }
    }
}
#endif
