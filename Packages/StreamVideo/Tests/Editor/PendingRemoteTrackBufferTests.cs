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
using Unity.WebRTC;
using UnityEngine.TestTools;
using Participant = StreamVideo.v1.Sfu.Models.Participant;
using SfuTrackType = StreamVideo.v1.Sfu.Models.TrackType;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for pending remote track buffering in <see cref="RtcSession"/>.
    /// </summary>
    internal sealed class PendingRemoteTrackBufferTests
    {
        private const string CallCid = "test:call";
        private const string RemoteSessionId = "remote-session";
        private const string RemoteUserId = "remote-user";
        private const string TrackPrefix = "abc123";
        private const string VideoTrackTypeKey = "TRACK_TYPE_VIDEO";

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
            _timeService = Substitute.For<ITimeService>();
            _timeService.Time.Returns(_ => _currentTime);

            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(Substitute.For<ISfuWebSocket>());

            _session = new PendingRemoteTrackBufferTestRtcSession(
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
        public IEnumerator When_stream_arrives_before_prefix_set_expect_buffered()
            => When_stream_arrives_before_prefix_set_expect_buffered_Async().RunAsIEnumerator();

        private Task When_stream_arrives_before_prefix_set_expect_buffered_Async()
        {
            SetupCallWithRemoteParticipant(trackLookupPrefix: string.Empty);
            InvokeSubscriberStreamAdded(CreateMediaStream($"{TrackPrefix}:{VideoTrackTypeKey}"));

            Assert.That(_session.BindAttempts, Is.Empty,
                "Stream should be buffered when TrackLookupPrefix is not yet known.");
            Assert.That(GetPendingTrackCount(), Is.EqualTo(1),
                "Expected one pending subscriber stream in the buffer.");
            return Task.CompletedTask;
        }

        [UnityTest]
        public IEnumerator When_participant_joined_expect_pending_stream_bound()
            => When_participant_joined_expect_pending_stream_bound_Async().RunAsIEnumerator();

        private Task When_participant_joined_expect_pending_stream_bound_Async()
        {
            var remoteParticipant = SetupCallWithRemoteParticipant(trackLookupPrefix: string.Empty);
            ConfigureParticipantCache(remoteParticipant);

            InvokeSubscriberStreamAdded(CreateMediaStream($"{TrackPrefix}:{VideoTrackTypeKey}"));
            Assert.That(GetPendingTrackCount(), Is.EqualTo(1),
                "Precondition: stream should be buffered before ParticipantJoined.");

            InvokeParticipantJoined(new ParticipantJoined
            {
                CallCid = CallCid,
                Participant = new Participant
                {
                    SessionId = RemoteSessionId,
                    UserId = RemoteUserId,
                    TrackLookupPrefix = TrackPrefix,
                },
            });

            Assert.That(_session.BindAttempts, Has.Count.EqualTo(1),
                "Pending stream should bind after ParticipantJoined sets TrackLookupPrefix.");
            Assert.That(_session.BindAttempts[0].TrackPrefix, Is.EqualTo(TrackPrefix),
                "Bound stream should use the participant track prefix.");
            Assert.That(GetPendingTrackCount(), Is.EqualTo(0),
                "Buffer should be empty after a successful drain.");
            Assert.That(remoteParticipant.TrackLookupPrefix, Is.EqualTo(TrackPrefix),
                "ParticipantJoined should hydrate TrackLookupPrefix.");
            return Task.CompletedTask;
        }

        [UnityTest]
        public IEnumerator When_track_published_with_participant_dto_expect_pending_stream_bound()
            => When_track_published_with_participant_dto_expect_pending_stream_bound_Async().RunAsIEnumerator();

        private Task When_track_published_with_participant_dto_expect_pending_stream_bound_Async()
        {
            // Large-call optimization: the SFU skips ParticipantJoined and only embeds the participant on the first
            // TrackPublished. The participant is therefore absent from the call when the subscriber stream arrives.
            var call = CreateEmptyCall();
            _session.ActiveCall = call;

            var remoteParticipant = CreateRemoteParticipant(RemoteSessionId, trackLookupPrefix: string.Empty);
            ConfigureParticipantCache(remoteParticipant);

            InvokeSubscriberStreamAdded(CreateMediaStream($"{TrackPrefix}:{VideoTrackTypeKey}"));
            Assert.That(GetPendingTrackCount(), Is.EqualTo(1),
                "Precondition: stream should be buffered before TrackPublished.");
            Assert.That(call.Participants, Is.Empty,
                "Precondition: no participant should exist before TrackPublished in the large-call path.");

            InvokeTrackPublished(new TrackPublished
            {
                UserId = RemoteUserId,
                SessionId = RemoteSessionId,
                Type = SfuTrackType.Video,
                Participant = new Participant
                {
                    SessionId = RemoteSessionId,
                    UserId = RemoteUserId,
                    TrackLookupPrefix = TrackPrefix,
                },
            });

            Assert.That(call.Participants, Has.Count.EqualTo(1),
                "TrackPublished with embedded DTO should materialize the participant (large-call path).");
            Assert.That(_session.BindAttempts, Has.Count.EqualTo(1),
                "Pending stream should bind after TrackPublished embeds participant prefix.");
            Assert.That(GetPendingTrackCount(), Is.EqualTo(0),
                "Buffer should be empty after TrackPublished drain.");
            Assert.That(remoteParticipant.TrackLookupPrefix, Is.EqualTo(TrackPrefix),
                "TrackPublished participant DTO should hydrate TrackLookupPrefix.");
            return Task.CompletedTask;
        }

        [UnityTest]
        public IEnumerator When_participant_already_has_prefix_expect_immediate_bind()
            => When_participant_already_has_prefix_expect_immediate_bind_Async().RunAsIEnumerator();

        private Task When_participant_already_has_prefix_expect_immediate_bind_Async()
        {
            SetupCallWithRemoteParticipant(trackLookupPrefix: TrackPrefix);
            InvokeSubscriberStreamAdded(CreateMediaStream($"{TrackPrefix}:{VideoTrackTypeKey}"));

            Assert.That(_session.BindAttempts, Has.Count.EqualTo(1),
                "Stream should bind immediately when TrackLookupPrefix is already known.");
            Assert.That(GetPendingTrackCount(), Is.EqualTo(0),
                "Buffer should remain empty on immediate bind.");
            return Task.CompletedTask;
        }

        [UnityTest]
        public IEnumerator When_stream_id_has_track_suffix_expect_immediate_bind()
            => When_stream_id_has_track_suffix_expect_immediate_bind_Async().RunAsIEnumerator();

        private Task When_stream_id_has_track_suffix_expect_immediate_bind_Async()
        {
            SetupCallWithRemoteParticipant(trackLookupPrefix: TrackPrefix);
            InvokeSubscriberStreamAdded(CreateMediaStream($"{TrackPrefix}:{VideoTrackTypeKey}:tR"));

            Assert.That(_session.BindAttempts, Has.Count.EqualTo(1),
                "Stream IDs with an optional track suffix should still bind.");
            Assert.That(_session.BindAttempts[0].TrackTypeKey, Is.EqualTo(VideoTrackTypeKey));
            Assert.That(GetPendingTrackCount(), Is.EqualTo(0));
            return Task.CompletedTask;
        }

        private StreamVideoCallParticipant SetupCallWithRemoteParticipant(string trackLookupPrefix)
        {
            var call = CreateCallWithRemoteParticipant(RemoteSessionId, trackLookupPrefix);
            _session.ActiveCall = call;
            return GetRemoteParticipant(call);
        }

        private void ConfigureParticipantCache(StreamVideoCallParticipant remoteParticipant)
        {
            var cache = Substitute.For<ICache>();
            var participantsRepo = Substitute.For<ICacheRepository<StreamVideoCallParticipant>>();
            cache.CallParticipants.Returns(participantsRepo);
            participantsRepo
                .CreateOrUpdate<StreamVideoCallParticipant, Participant>(Arg.Any<Participant>(), out _)
                .Returns(callInfo =>
                {
                    var dto = callInfo.Arg<Participant>();
                    remoteParticipant.UpdateFromSfu(dto);
                    callInfo[1] = false;
                    return remoteParticipant;
                });

            _session.SetCache(cache);
        }

        private void InvokeSubscriberStreamAdded(MediaStream mediaStream)
            => InvokeSfuHandler("OnSubscriberStreamAdded", mediaStream);

        private void InvokeParticipantJoined(ParticipantJoined participantJoined)
            => InvokeSfuHandler("OnSfuParticipantJoined", participantJoined);

        private void InvokeTrackPublished(TrackPublished trackPublished)
            => InvokeSfuHandler("OnSfuTrackPublished", trackPublished);

        private void InvokeSfuHandler(string methodName, object eventArg)
        {
            var method = typeof(RtcSession).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected private handler `{methodName}` on {nameof(RtcSession)}.");
            method.Invoke(_session, new[] { eventArg });
        }

        private int GetPendingTrackCount()
        {
            var field = typeof(RtcSession).GetField("_pendingTracks",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected pending track buffer field on RtcSession.");
            return ((IReadOnlyCollection<object>)field.GetValue(_session)).Count;
        }

        private static MediaStream CreateMediaStream(string streamId)
        {
            var context = GetWebRtcContext();
            var ptr = (IntPtr)context.GetType()
                .GetMethod("CreateMediaStream", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(context, new object[] { streamId });
            return (MediaStream)Activator.CreateInstance(
                typeof(MediaStream),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { ptr },
                culture: null);
        }

        private static object GetWebRtcContext()
        {
            var contextProperty = typeof(WebRTC).GetProperty("Context",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(contextProperty, Is.Not.Null, "Expected internal WebRTC.Context property.");
            return contextProperty.GetValue(null);
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

        private StreamCall CreateEmptyCall()
        {
            var call = new StreamCall(CallCid,
                Substitute.For<ICacheRepository<StreamCall>>(),
                Substitute.For<IStatefulModelContext>());

            typeof(StreamCall)
                .GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(call, new CallSession());

            return call;
        }

        private StreamVideoCallParticipant CreateRemoteParticipant(string remoteSessionId, string trackLookupPrefix)
        {
            var participantContext = CreateParticipantContext(_session);
            var participant = new StreamVideoCallParticipant(
                remoteSessionId,
                Substitute.For<ICacheRepository<StreamVideoCallParticipant>>(),
                participantContext);

            ((IUpdateableFrom<Participant, StreamVideoCallParticipant>)participant).UpdateFromDto(
                new Participant
                {
                    SessionId = remoteSessionId,
                    UserId = RemoteUserId,
                    TrackLookupPrefix = trackLookupPrefix ?? string.Empty,
                },
                participantContext.Cache);

            return participant;
        }

        private static void AddParticipantToCall(StreamCall call, StreamVideoCallParticipant participant)
        {
            var callSession = (CallSession)typeof(StreamCall)
                .GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(call);
            var participants = (List<StreamVideoCallParticipant>)typeof(CallSession)
                .GetField("_participants", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(callSession);
            participants.Add(participant);
        }

        private StreamCall CreateCallWithRemoteParticipant(string remoteSessionId, string trackLookupPrefix)
        {
            var call = CreateEmptyCall();
            AddParticipantToCall(call, CreateRemoteParticipant(remoteSessionId, trackLookupPrefix));
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
        private PendingRemoteTrackBufferTestRtcSession _session;

        private readonly struct CapturedBindAttempt
        {
            public CapturedBindAttempt(string trackPrefix, string trackTypeKey, string sessionId)
            {
                TrackPrefix = trackPrefix;
                TrackTypeKey = trackTypeKey;
                SessionId = sessionId;
            }

            public string TrackPrefix { get; }
            public string TrackTypeKey { get; }
            public string SessionId { get; }
        }

        private sealed class PendingRemoteTrackBufferTestRtcSession : RtcSession
        {
            public List<CapturedBindAttempt> BindAttempts { get; } = new List<CapturedBindAttempt>();

            public PendingRemoteTrackBufferTestRtcSession(ISfuWebSocketFactory sfuWebSocketFactory,
                Func<IStreamCall, HttpClient> httpClientFactory,
                ILogs logs, ISerializer serializer, ITimeService timeService,
                StreamVideoLowLevelClient lowLevelClient,
                IStreamClientConfig config, INetworkMonitor networkMonitor)
                : base(sfuWebSocketFactory, httpClientFactory, logs, serializer,
                    timeService, lowLevelClient, config, networkMonitor)
            {
            }

            protected override bool BindSubscriberStream(StreamVideoCallParticipant participant, string trackTypeKey,
                MediaStream mediaStream)
            {
                BindAttempts.Add(new CapturedBindAttempt(participant.TrackLookupPrefix, trackTypeKey,
                    participant.SessionId));
                return true;
            }
        }
    }
}
#endif
