#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using StreamVideo.v1.Sfu.Events;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;
using UnityEngine.TestTools;
using PublishTrackType = StreamVideo.Core.Models.Sfu.TrackType;
using SfuTrackType = StreamVideo.v1.Sfu.Models.TrackType;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for publish-gated video subscription logic in <see cref="RtcSession"/>.
    /// </summary>
    internal sealed class PublishGatedVideoSubscriptionTests
    {
        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
            _timeService = Substitute.For<ITimeService>();
            _timeService.Time.Returns(_ => _currentTime);

            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(Substitute.For<ISfuWebSocket>());

            _session = new PublishGatedVideoSubscriptionTestRtcSession(
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
        public IEnumerator When_incoming_video_requested_but_peer_not_publishing_expect_no_video_in_update_subscriptions()
            => When_incoming_video_requested_but_peer_not_publishing_expect_no_video_in_update_subscriptions_Async()
                .RunAsIEnumerator();

        private async Task
            When_incoming_video_requested_but_peer_not_publishing_expect_no_video_in_update_subscriptions_Async()
        {
            SetupCallWithRemoteParticipant("remote-session", publishedTracks: new[] { PublishTrackType.Audio });
            _session.UpdateIncomingVideoRequested("remote-session", true);
            _session.UpdateIncomingAudioRequested("remote-session", true);

            await SendUpdateSubscriptionsAsync();

            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Video), Is.False,
                "Video must not be requested when the peer is not publishing it, even if incoming video is enabled.");
            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Audio), Is.True,
                "Audio should still be requested when incoming audio is enabled.");
        }

        [UnityTest]
        public IEnumerator When_peer_publishes_video_expect_video_in_update_subscriptions()
            => When_peer_publishes_video_expect_video_in_update_subscriptions_Async().RunAsIEnumerator();

        private async Task When_peer_publishes_video_expect_video_in_update_subscriptions_Async()
        {
            var participant = SetupCallWithRemoteParticipant("remote-session",
                publishedTracks: new[] { PublishTrackType.Audio, PublishTrackType.Video });
            _session.UpdateIncomingVideoRequested("remote-session", true);

            await SendUpdateSubscriptionsAsync();

            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Video), Is.True,
                "Video should be requested when the peer is publishing and incoming video is enabled.");
            Assert.That(participant.IsPublishingTrack(PublishTrackType.Video), Is.True,
                "Participant publish state should reflect the SFU published tracks.");
        }

        [UnityTest]
        public IEnumerator When_track_published_without_participant_dto_expect_video_in_follow_up_update_subscriptions()
            => When_track_published_without_participant_dto_expect_video_in_follow_up_update_subscriptions_Async()
                .RunAsIEnumerator();

        private async Task
            When_track_published_without_participant_dto_expect_video_in_follow_up_update_subscriptions_Async()
        {
            var participant = SetupCallWithRemoteParticipant("remote-session",
                publishedTracks: new[] { PublishTrackType.Audio });
            _session.UpdateIncomingVideoRequested("remote-session", true);

            await SendUpdateSubscriptionsAsync();
            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Video), Is.False,
                "Late publish scenario should not include video before the TrackPublished event.");

            InvokeTrackPublished(new TrackPublished
            {
                UserId = "remote-user",
                SessionId = "remote-session",
                Type = SfuTrackType.Video,
            });

            await SendUpdateSubscriptionsAsync();

            Assert.That(participant.IsPublishingTrack(PublishTrackType.Video), Is.True,
                "TrackPublished without participant DTO must still update PublishedTracks.");
            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Video), Is.True,
                "A follow-up UpdateSubscriptions must include video once the peer starts publishing.");
        }

        [UnityTest]
        public IEnumerator When_peer_unpublishes_video_expect_video_removed_from_update_subscriptions()
            => When_peer_unpublishes_video_expect_video_removed_from_update_subscriptions_Async()
                .RunAsIEnumerator();

        private async Task When_peer_unpublishes_video_expect_video_removed_from_update_subscriptions_Async()
        {
            var participant = SetupCallWithRemoteParticipant("remote-session",
                publishedTracks: new[] { PublishTrackType.Audio, PublishTrackType.Video });
            _session.UpdateIncomingVideoRequested("remote-session", true);
            _session.UpdateIncomingAudioRequested("remote-session", true);

            await SendUpdateSubscriptionsAsync();
            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Video), Is.True,
                "Precondition: video should be subscribed while the peer is publishing.");
            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Audio), Is.True,
                "Precondition: audio should be subscribed while the peer is publishing.");

            InvokeTrackUnpublished(new TrackUnpublished
            {
                UserId = "remote-user",
                SessionId = "remote-session",
                Type = SfuTrackType.Video,
            });

            await SendUpdateSubscriptionsAsync();

            Assert.That(participant.IsPublishingTrack(PublishTrackType.Video), Is.False,
                "TrackUnpublished without participant DTO must remove video from PublishedTracks.");
            Assert.That(ContainsTrackType(_session.LastRequestedTracks, SfuTrackType.Video), Is.False,
                "Remote camera mute must drop video from UpdateSubscriptions while incoming video remains enabled.");
            Assert.That(
                _session.LastRequestedTracks.Any(t =>
                    t.SessionId == "remote-session" && t.TrackType == SfuTrackType.Audio),
                Is.True,
                "Audio subscription should remain after the peer unpublishes video.");
        }

        private async Task SendUpdateSubscriptionsAsync()
        {
            var expectedCount = _session.UpdateSubscriptionsCallCount + 1;
            AdvanceTimePastDebounce();
            _session.Update();

            await TestUtils.WaitUntilAsync(() => _session.UpdateSubscriptionsCallCount >= expectedCount,
                "UpdateSubscriptions RPC should be sent.");
        }

        private StreamVideoCallParticipant SetupCallWithRemoteParticipant(string remoteSessionId,
            IEnumerable<PublishTrackType> publishedTracks)
        {
            var call = CreateCallWithRemoteParticipant(_session, remoteSessionId, publishedTracks);
            _session.ActiveCall = call;
            return GetRemoteParticipant(call);
        }

        private void InvokeTrackPublished(TrackPublished trackPublished)
            => InvokeSfuHandler("OnSfuTrackPublished", trackPublished);

        private void InvokeTrackUnpublished(TrackUnpublished trackUnpublished)
            => InvokeSfuHandler("OnSfuTrackUnpublished", trackUnpublished);

        private void InvokeSfuHandler(string methodName, object eventArg)
        {
            var method = typeof(RtcSession).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected private handler `{methodName}` on {nameof(RtcSession)}.");
            method.Invoke(_session, new[] { eventArg });
        }

        private void AdvanceTimePastDebounce()
        {
            _currentTime += TrackSubscriptionDebounceTime + 0.01f;
        }

        private static bool ContainsTrackType(IReadOnlyList<CapturedTrackSubscription> tracks, SfuTrackType trackType)
            => tracks.Any(t => t.TrackType == trackType);

        private readonly struct CapturedTrackSubscription
        {
            public CapturedTrackSubscription(string sessionId, SfuTrackType trackType)
            {
                SessionId = sessionId;
                TrackType = trackType;
            }

            public string SessionId { get; }
            public SfuTrackType TrackType { get; }
        }

        private static StreamVideoCallParticipant GetRemoteParticipant(StreamCall call)
        {
            var callSession = (CallSession)typeof(StreamCall)
                .GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(call);
            var participants = (List<StreamVideoCallParticipant>)typeof(CallSession)
                .GetField("_participants", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(callSession);
            return participants.Single();
        }

        private static StreamCall CreateCallWithRemoteParticipant(RtcSession session, string remoteSessionId,
            IEnumerable<PublishTrackType> publishedTracks)
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

            foreach (var publishedTrack in publishedTracks)
            {
                participant.AddPublishedTrack(publishedTrack);
            }

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
        private PublishGatedVideoSubscriptionTestRtcSession _session;

        private const float TrackSubscriptionDebounceTime = 0.1f;

        private sealed class PublishGatedVideoSubscriptionTestRtcSession : RtcSession
        {
            public int UpdateSubscriptionsCallCount { get; private set; }
            public IReadOnlyList<CapturedTrackSubscription> LastRequestedTracks { get; private set; }
                = Array.Empty<CapturedTrackSubscription>();

            public PublishGatedVideoSubscriptionTestRtcSession(ISfuWebSocketFactory sfuWebSocketFactory,
                Func<IStreamCall, HttpClient> httpClientFactory,
                ILogs logs, ISerializer serializer, ITimeService timeService,
                StreamVideoLowLevelClient lowLevelClient,
                IStreamClientConfig config, INetworkMonitor networkMonitor)
                : base(sfuWebSocketFactory, httpClientFactory, logs, serializer,
                    timeService, lowLevelClient, config, networkMonitor)
            {
            }

            protected override Task<UpdateSubscriptionsResponse> SendUpdateSubscriptionsAsync(
                UpdateSubscriptionsRequest request, CancellationToken cancellationToken)
            {
                UpdateSubscriptionsCallCount++;
                LastRequestedTracks = CaptureTrackSubscriptions(request);
                return Task.FromResult(new UpdateSubscriptionsResponse());
            }

            private static List<CapturedTrackSubscription> CaptureTrackSubscriptions(object request)
            {
                var result = new List<CapturedTrackSubscription>();
                var tracks = (IEnumerable)request.GetType()
                    .GetProperty("Tracks")!
                    .GetValue(request)!;

                foreach (var track in tracks)
                {
                    var trackType = (SfuTrackType)track.GetType()
                        .GetProperty("TrackType")!
                        .GetValue(track)!;
                    var sessionId = (string)track.GetType()
                        .GetProperty("SessionId")!
                        .GetValue(track)!;
                    result.Add(new CapturedTrackSubscription(sessionId, trackType));
                }

                return result;
            }
        }
    }
}
#endif
