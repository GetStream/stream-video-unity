#if STREAM_TESTS_ENABLED
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using StreamVideo.v1.Sfu.Events;
using PublicConnectionQuality = StreamVideo.Core.Models.Sfu.ConnectionQuality;
using PublishTrackType = StreamVideo.Core.Models.Sfu.TrackType;
using SfuConnectionQuality = StreamVideo.v1.Sfu.Models.ConnectionQuality;
using SfuParticipant = StreamVideo.v1.Sfu.Models.Participant;
using SfuTrackType = StreamVideo.v1.Sfu.Models.TrackType;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for dominant speaker, live media state, and local microphone reporting on
    /// <see cref="StreamCall"/> and <see cref="StreamVideoCallParticipant"/>.
    /// </summary>
    internal sealed class ParticipantIndicatorTests
    {
        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<ISfuWebSocketFactory>();
            factory.Create().Returns(Substitute.For<ISfuWebSocket>());

            _session = new RtcSession(
                sfuWebSocketFactory: factory,
                httpClientFactory: _ => null,
                logs: Substitute.For<ILogs>(),
                serializer: Substitute.For<ISerializer>(),
                timeService: Substitute.For<ITimeService>(),
                lowLevelClient: null,
                config: StreamClientConfig.Default,
                networkMonitor: Substitute.For<INetworkMonitor>());
            _session.SessionId.Regenerate();

            _context = Substitute.For<IStatefulModelContext>();
            _context.Cache.Returns(Substitute.For<ICache>());
            _context.Logs.Returns(Substitute.For<ILogs>());
            _context.Serializer.Returns(Substitute.For<ISerializer>());

            var client = Substitute.For<IInternalStreamVideoClient>();
            client.InternalLowLevelClient.Returns(CreateLowLevelClientShim(_session));
            _context.Client.Returns(client);
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
        }

        [Test]
        public void When_dominant_speaker_changes_expect_only_the_matching_participant_flagged()
        {
            var first = CreateParticipant("session-a", "user-a");
            var second = CreateParticipant("session-b", "user-b");
            var call = CreateCall(first, second);

            IStreamVideoCallParticipant current = null;
            IStreamVideoCallParticipant previous = null;
            call.DominantSpeakerChanged += (next, prev) =>
            {
                current = next;
                previous = prev;
            };

            Elect(call, first);
            Elect(call, second);

            Assert.That(first.IsDominantSpeaker, Is.False,
                "The previous dominant speaker flag must be cleared when someone else is elected.");
            Assert.That(second.IsDominantSpeaker, Is.True,
                "Only the participant named by DominantSpeakerChanged is the dominant speaker.");
            Assert.That(call.DominantSpeaker, Is.SameAs(second),
                "IStreamCall.DominantSpeaker must be the participant named by the latest event.");
            Assert.That(call.DominantSpeaker.SessionId, Is.EqualTo(second.SessionId),
                "The call-level dominant speaker must be the elected session.");
            Assert.That(current, Is.SameAs(second),
                "DominantSpeakerChanged must report the newly elected participant.");
            Assert.That(previous, Is.SameAs(first),
                "DominantSpeakerChanged must report the participant who was dominant before.");
            Assert.That(call.SortedParticipants.First(), Is.SameAs(second),
                "SortedParticipants must put the live dominant speaker first.");
        }

        [Test]
        public void When_dominant_speaker_event_matches_nobody_expect_all_flags_cleared_and_dominant_speaker_null()
        {
            var first = CreateParticipant("session-a", "user-a");
            var second = CreateParticipant("session-b", "user-b");
            var call = CreateCall(first, second);
            Elect(call, first);

            Elect(call, "missing-session", "missing-user");

            Assert.That(first.IsDominantSpeaker, Is.False,
                "An unknown dominant-speaker session must clear the previous flag.");
            Assert.That(second.IsDominantSpeaker, Is.False,
                "An unknown dominant-speaker session must leave every participant not dominant.");
            Assert.That(call.DominantSpeaker, Is.Null,
                "DominantSpeaker must be null when the event names a session that is not in the call.");
        }

        [Test]
        public void When_no_dominant_speaker_event_yet_expect_dominant_speaker_null()
        {
            var first = CreateParticipant("session-a", "user-a");
            var call = CreateCall(first, CreateParticipant("session-b", "user-b"));

            Assert.That(call.Participants, Is.Not.Null.And.Not.Empty,
                "Precondition: participants are present, so a first-participant fallback would be visible.");
            Assert.That(call.DominantSpeaker, Is.Null,
                "DominantSpeaker must stay null until a DominantSpeakerChanged event matches a participant.");
            Assert.That(call.DominantSpeaker, Is.Not.SameAs(first),
                "DominantSpeaker must not fall back to the first participant.");
        }

        [Test]
        public void When_track_unpublished_participant_snapshot_arrives_expect_speaking_level_dominant_and_quality_preserved()
        {
            var participant = CreateParticipant("speaker-session", "speaker-user", "Before");
            SetLiveMediaState(participant);
            participant.AddPublishedTrack(PublishTrackType.Audio);
            participant.AddPublishedTrack(PublishTrackType.Video);
            CreateCall(participant);

            var snapshot = ContradictingSnapshot(participant, "After");
            InvokeSfuHandler("OnSfuTrackUnpublished", new TrackUnpublished
            {
                UserId = participant.UserId,
                SessionId = participant.SessionId,
                Type = SfuTrackType.Audio,
                Participant = snapshot,
            });

            AssertLiveMediaStatePreserved(participant);
            Assert.That(participant.Name, Is.EqualTo("After"),
                "TrackUnpublished must still apply identity fields from the embedded participant.");
            Assert.That(participant.IsPublishingTrack(PublishTrackType.Audio), Is.False,
                "Unpublishing audio must remove the audio track from PublishedTracks.");
            Assert.That(participant.IsPublishingTrack(PublishTrackType.Video), Is.True,
                "Unpublishing audio must leave the other published tracks in place.");
        }

        [Test]
        public void When_participant_joined_expect_realtime_fields_copied_from_the_dto()
        {
            var participant = CreateParticipant("joined-session", "joined-user");
            var cache = Substitute.For<ICache>();
            var repository = Substitute.For<ICacheRepository<StreamVideoCallParticipant>>();
            cache.CallParticipants.Returns(repository);
            repository
                .CreateOrUpdate<StreamVideoCallParticipant, SfuParticipant>(Arg.Any<SfuParticipant>(), out _)
                .Returns(callInfo =>
                {
                    ((IUpdateableFrom<SfuParticipant, StreamVideoCallParticipant>)participant)
                        .UpdateFromDto(callInfo.Arg<SfuParticipant>(), cache);
                    callInfo[1] = true;
                    return participant;
                });

            var joined = new SfuParticipant
            {
                SessionId = participant.SessionId,
                UserId = participant.UserId,
                Name = "Joined",
                IsSpeaking = true,
                IsDominantSpeaker = true,
                AudioLevel = 0.42f,
                ConnectionQuality = SfuConnectionQuality.Excellent,
            };

            new CallSession().UpdateFromSfu(new ParticipantJoined { Participant = joined }, cache);

            Assert.That(participant.IsSpeaking, Is.True,
                "ParticipantJoined must initialize IsSpeaking from the SFU participant.");
            Assert.That(participant.IsDominantSpeaker, Is.True,
                "ParticipantJoined must initialize IsDominantSpeaker from the SFU participant.");
            Assert.That(participant.AudioLevel, Is.EqualTo(0.42f),
                "ParticipantJoined must initialize AudioLevel from the SFU participant.");
            Assert.That(participant.ConnectionQuality, Is.EqualTo(PublicConnectionQuality.Excellent),
                "ParticipantJoined must initialize ConnectionQuality from the SFU participant.");
            Assert.That(participant.Name, Is.EqualTo("Joined"),
                "ParticipantJoined must still copy identity fields.");
        }

        [Test]
        public void When_audio_level_event_arrives_expect_speaking_and_level_updated_for_that_session_only()
        {
            var first = CreateParticipant("session-a", "user-a");
            var second = CreateParticipant("session-b", "user-b");
            var call = CreateCall(first, second);
            first.UpdateFromSfu(new AudioLevel { IsSpeaking = true, Level = LiveAudioLevel });
            second.UpdateFromSfu(new AudioLevel { IsSpeaking = false, Level = 0f });

            var firstSpeakingChanges = 0;
            var secondSpeakingChanges = 0;
            var secondLevelChanges = 0;
            first.IsSpeakingChanged += _ => firstSpeakingChanges++;
            second.IsSpeakingChanged += _ => secondSpeakingChanges++;
            second.AudioLevelChanged += _ => secondLevelChanges++;

            var changed = new AudioLevelChanged();
            changed.AudioLevels.Add(new AudioLevel
            {
                SessionId = second.SessionId,
                UserId = second.UserId,
                IsSpeaking = true,
                Level = 0.5f,
            });
            call.UpdateFromSfu(changed);

            Assert.That(first.IsSpeaking, Is.True,
                "AudioLevelChanged must not change IsSpeaking for a different session.");
            Assert.That(first.AudioLevel, Is.EqualTo(LiveAudioLevel),
                "AudioLevelChanged must not change AudioLevel for a different session.");
            Assert.That(firstSpeakingChanges, Is.EqualTo(0),
                "IsSpeakingChanged must not fire for a session the event did not include.");
            Assert.That(second.IsSpeaking, Is.True,
                "IsSpeaking must follow AudioLevelChanged for the matching session.");
            Assert.That(second.AudioLevel, Is.EqualTo(0.5f),
                "AudioLevel must follow AudioLevelChanged for the matching session.");
            Assert.That(secondSpeakingChanges, Is.EqualTo(1),
                "IsSpeakingChanged must fire once when that session starts speaking.");
            Assert.That(secondLevelChanges, Is.EqualTo(1),
                "AudioLevelChanged must fire once when that session's level changes.");
        }

        [Test]
        public void When_participant_updated_profile_arrives_expect_speaking_level_dominant_and_quality_preserved()
        {
            var cache = Substitute.For<ICache>();
            var repository = new CacheRepository<StreamVideoCallParticipant>(_ => null, cache);
            cache.CallParticipants.Returns(repository);
            _session.SetCache(cache);

            var participant = new StreamVideoCallParticipant(
                "profile-session", repository, _context);
            ((IUpdateableFrom<SfuParticipant, StreamVideoCallParticipant>)participant).UpdateFromDto(
                new SfuParticipant
                {
                    SessionId = "profile-session",
                    UserId = "profile-user",
                    Name = "Before",
                },
                _context.Cache);
            SetLiveMediaState(participant);

            InvokeSfuHandler("OnSfuWebSocketOnParticipantUpdated", new ParticipantUpdated
            {
                Participant = ContradictingSnapshot(participant, "After"),
            });

            AssertLiveMediaStatePreserved(participant);
            Assert.That(participant.Name, Is.EqualTo("After"),
                "ParticipantUpdated must still apply the profile name.");
        }

        [Test]
        public void When_local_mic_disabled_expect_local_is_audio_enabled_false_without_an_audio_track()
        {
            var local = CreateParticipant(_session.SessionId.ToString(), "local-user");

            Assert.That(local.IsLocalParticipant, Is.True,
                "Precondition: the participant session matches this device.");
            Assert.That(local.AudioTrack, Is.Null,
                "The local participant has no received audio track.");

            _session.TrySetPublisherAudioTrackEnabled(true);
            Assert.That(local.IsAudioEnabled, Is.True,
                "Local IsAudioEnabled must follow the publisher microphone while it is on.");

            _session.TrySetPublisherAudioTrackEnabled(false);
            Assert.That(local.IsAudioEnabled, Is.False,
                "Local IsAudioEnabled must be false as soon as the publisher microphone is disabled.");
            Assert.That(local.AudioTrack, Is.Null,
                "Disabling the local microphone must not require a received audio track.");
        }

        private void Elect(StreamCall call, StreamVideoCallParticipant participant)
            => Elect(call, participant.SessionId, participant.UserId);

        private static void Elect(StreamCall call, string sessionId, string userId)
        {
            call.UpdateFromSfu(new DominantSpeakerChanged
            {
                SessionId = sessionId,
                UserId = userId,
            }, null);
        }

        private static void SetLiveMediaState(StreamVideoCallParticipant participant)
        {
            participant.UpdateFromSfu(new AudioLevel { IsSpeaking = true, Level = LiveAudioLevel });
            participant.UpdateFromSfu(new ConnectionQualityInfo
            {
                ConnectionQuality = SfuConnectionQuality.Good,
            });
        }

        private static void AssertLiveMediaStatePreserved(StreamVideoCallParticipant participant)
        {
            Assert.That(participant.IsSpeaking, Is.True,
                "A track or profile snapshot must not clear live IsSpeaking.");
            Assert.That(participant.AudioLevel, Is.EqualTo(LiveAudioLevel),
                "A track or profile snapshot must not replace a live audio level.");
            Assert.That(participant.IsDominantSpeaker, Is.False,
                "A track or profile snapshot must not elect the dominant speaker.");
            Assert.That(participant.ConnectionQuality, Is.EqualTo(PublicConnectionQuality.Good),
                "A track or profile snapshot must not replace live connection quality.");
        }

        private static SfuParticipant ContradictingSnapshot(StreamVideoCallParticipant participant, string name)
        {
            var snapshot = new SfuParticipant
            {
                SessionId = participant.SessionId,
                UserId = participant.UserId,
                Name = name,
                IsSpeaking = false,
                IsDominantSpeaker = true,
                AudioLevel = 0f,
                ConnectionQuality = SfuConnectionQuality.Good,
            };
            snapshot.PublishedTracks.Add(SfuTrackType.Video);
            return snapshot;
        }

        private StreamVideoCallParticipant CreateParticipant(string sessionId, string userId, string name = "")
        {
            var participant = new StreamVideoCallParticipant(
                sessionId,
                Substitute.For<ICacheRepository<StreamVideoCallParticipant>>(),
                _context);

            ((IUpdateableFrom<SfuParticipant, StreamVideoCallParticipant>)participant).UpdateFromDto(
                new SfuParticipant
                {
                    SessionId = sessionId,
                    UserId = userId,
                    Name = name,
                },
                _context.Cache);

            return participant;
        }

        private StreamCall CreateCall(params StreamVideoCallParticipant[] participants)
        {
            var call = new StreamCall("test:call",
                Substitute.For<ICacheRepository<StreamCall>>(),
                _context);
            var callSession = new CallSession();
            GetPrivateField<List<StreamVideoCallParticipant>>(callSession, "_participants").AddRange(participants);
            SetPrivateField(call, "_session", callSession);
            _session.ActiveCall = call;
            return call;
        }

        private void InvokeSfuHandler(string methodName, object eventArg)
        {
            var method = typeof(RtcSession).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected private handler `{methodName}` on {nameof(RtcSession)}.");
            method.Invoke(_session, new[] { eventArg });
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field {fieldName} on {target.GetType().Name}.");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static StreamVideoLowLevelClient CreateLowLevelClientShim(RtcSession session)
        {
            var lowLevelClient = (StreamVideoLowLevelClient)RuntimeHelpers.GetUninitializedObject(
                typeof(StreamVideoLowLevelClient));

            var backingField = typeof(StreamVideoLowLevelClient).GetField("<RtcSession>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(backingField, Is.Not.Null, "Expected the RtcSession property backing field.");
            backingField.SetValue(lowLevelClient, session);

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

        private const float LiveAudioLevel = 0.286f;

        private RtcSession _session;
        private IStatefulModelContext _context;
    }
}
#endif
