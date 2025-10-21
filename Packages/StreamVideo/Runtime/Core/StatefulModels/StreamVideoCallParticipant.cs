using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Serialization;
using Unity.WebRTC;
using UnityEngine;
using Participant = StreamVideo.v1.Sfu.Models.Participant;

namespace StreamVideo.Core.StatefulModels
{
    internal sealed class StreamVideoCallParticipant : StreamStatefulModelBase<StreamVideoCallParticipant>,
        IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>,
        IUpdateableFrom<Participant, StreamVideoCallParticipant>,
        IStreamVideoCallParticipant
    {
        public event ParticipantTrackChangedHandler TrackAdded;

        public bool IsLocalParticipant => UserSessionId == Client.InternalLowLevelClient.RtcSession.SessionId;

        public bool IsPinned { get; private set; }

        public bool IsScreenSharing => ScreenShareTrack?.IsEnabled ?? false;

        public bool IsVideoEnabled => VideoTrack?.IsEnabled ?? false;
        public bool IsAudioEnabled => AudioTrack?.IsEnabled ?? false;

        #region Tracks

        //StreamTodo: VideoTrack, AudioTrack, ScreenShareTrack should be more specific types. Otherwise developer would have to usually cast it so there's no point in not doing this already
        public IStreamTrack AudioTrack => _audioTrack;
        public IStreamTrack VideoTrack => _videoTrack;
        public IStreamTrack ScreenShareTrack => _screenShareTrack;

        #endregion

        #region State

        public DateTimeOffset JoinedAt { get; private set; }

        public string Role { get; private set; }

        public IStreamVideoUser User { get; set; }

        //StreamTODO: investigate why we have UserSessionID and SessionId. On a user that was joining the call I had null SessionId while UserSessionId had value
        // They probably represent the same thing and one is set by coordinator and the other by SFU but let's verify
        public string UserSessionId
        {
            get => _userSessionId;
            private set
            {
                Logs.WarningIfDebug($"UserSessionId set to: {value} for participant: {UserId} and Session ID: {SessionId}");
                _userSessionId = value;

                if (string.IsNullOrEmpty(SessionId))
                {
                    SessionId = value;
                }
            }
        }

        private string _userSessionId;

        #endregion

        #region Sfu State

        public string UserId { get; private set; }

        public string SessionId { get; private set; }
        public IEnumerable<TrackType> PublishedTracks => _publishedTracks;

        public string TrackLookupPrefix { get; private set; }
        public ConnectionQuality ConnectionQuality { get; private set; }
        public bool IsSpeaking { get; private set; }
        public bool IsDominantSpeaker { get; private set; }
        public float AudioLevel { get; private set; }
        public string Name { get; private set; }
        public string Image { get; private set; }
        public IEnumerable<string> Roles => _roles;

        #endregion

        public IStreamCustomData CustomData => InternalCustomData;

        public StreamVideoCallParticipant(string uniqueId, ICacheRepository<StreamVideoCallParticipant> repository,
            IStatefulModelContext context)
            : base(uniqueId, repository, context)
        {
        }

        public IEnumerable<IStreamTrack> GetTracks()
        {
            if (_audioTrack != null)
            {
                yield return _audioTrack;
            }

            if (_videoTrack != null)
            {
                yield return _videoTrack;
            }

            if (_screenShareTrack != null)
            {
                yield return _screenShareTrack;
            }
        }

        public void UpdateRequestedVideoResolution(VideoResolution videoResolution)
            => LowLevelClient.RtcSession.UpdateRequestedVideoResolution(SessionId, videoResolution);

        public override string ToString()
            => $"{nameof(StreamVideoCallParticipant)} with User ID: {UserId} & Session ID: {SessionId}";

        //StreamTodo: perhaps distinguish to UpdateFromSfu interface
        void IUpdateableFrom<Participant, StreamVideoCallParticipant>.UpdateFromDto(Participant dto, ICache cache)
        {
            UserId = dto.UserId;
            SessionId = dto.SessionId;
            _publishedTracks.TryReplaceEnumsFromDtoCollection(dto.PublishedTracks, TrackTypeExt.ToPublicEnum);
            TrackLookupPrefix = dto.TrackLookupPrefix;
            ConnectionQuality = dto.ConnectionQuality.ToPublicEnum();
            IsSpeaking = dto.IsSpeaking;
            IsDominantSpeaker = dto.IsDominantSpeaker;
            AudioLevel = dto.AudioLevel;
            Name = dto.Name;
            Image = dto.Image;
            _roles.TryReplaceValuesFromDto(dto.Roles);
        }

        void IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>.UpdateFromDto(
            CallParticipantResponseInternalDTO dto, ICache cache)
        {
            JoinedAt = dto.JoinedAt;
            Role = dto.Role;
            User = cache.TryCreateOrUpdate(dto.User);
            UserSessionId = dto.UserSessionId;
        }

        internal void LoadCustomDataFromOwningCallCustomData(Dictionary<string, object> participantCustomData)
        {
            if (participantCustomData == null || participantCustomData.Count == 0)
            {
                InternalCustomData.InternalDictionary.Clear();
                return;
            }

            InternalCustomData.ReplaceAllWith(participantCustomData);
            TrySetVideoTrackRotationAngle();
        }

        private void TrySetVideoTrackRotationAngle()
        {
            if (_videoTrack != null && CustomData.TryGet<int>(VideoRotationAngleKey, out var angle))
            {
                _videoTrack.VideoRotationAngle = angle;
            }
        }

        //StreamTodo: solve with a generic interface and best to be handled by cache layer
        internal void UpdateFromSfu(Participant dto)
        {
            ((IUpdateableFrom<Participant, StreamVideoCallParticipant>)this).UpdateFromDto(dto, Cache);
        }

        internal void Update()
        {
            _audioTrack?.Update();
            _videoTrack?.Update();
            _screenShareTrack?.Update();

            UploadLocalParticipantPublishedVideoRotationAngle();
        }

        //StreamTodo: solve this better. IL2CPP fails to generate C++ code for TryConvertTo and fails with
        //"Attempting to call method 'StreamVideo.Libs.Serialization.NewtonsoftJsonSerializer::TryConvertTo<System.Single>' for which no ahead of time (AOT) code was generated."
        internal static void Dummy()
        {
            var serializer = new NewtonsoftJsonSerializer();
            serializer.TryConvertTo<float>("aa");
            serializer.TryConvertTo<int>("aa");
        }

        internal void SetTrack(TrackType type, MediaStreamTrack mediaStreamTrack, out IStreamTrack streamTrack)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[Participant] Local: {IsLocalParticipant} Session ID: {SessionId} set track of type {type}");
#endif

            switch (type)
            {
                case TrackType.Unspecified:
                    throw new NotSupportedException();
                case TrackType.Audio:
                    streamTrack = _audioTrack = new StreamAudioTrack((AudioStreamTrack)mediaStreamTrack);
                    break;
                case TrackType.Video:
                    streamTrack = _videoTrack = new StreamVideoTrack((VideoStreamTrack)mediaStreamTrack);
                    TrySetVideoTrackRotationAngle();
                    break;
                case TrackType.ScreenShare:
                    streamTrack = _screenShareTrack = new StreamVideoTrack((VideoStreamTrack)mediaStreamTrack);
                    break;
                case TrackType.ScreenShareAudio:
                    streamTrack = _screenShareAudioTrack = new StreamAudioTrack((AudioStreamTrack)mediaStreamTrack);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            TrackAdded?.Invoke(this, streamTrack);
        }

        internal void SetTrackEnabled(TrackType type, bool enabled)
        {
            var streamTrack = GetStreamTrack(type);
            if (streamTrack == null)
            {
                // Not an error, sometimes we receive tracks info from the API before webRTC triggers onTrack event
                return;
            }

#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[Participant] Local: {IsLocalParticipant}, Session ID: {SessionId} set track enabled of type {type} to {enabled}");
#endif
            
            streamTrack.SetEnabled(enabled);

            //StreamTodo: we should trigger some event that track status changed
        }

        internal void SetIsPinned(bool isPinned) => IsPinned = isPinned;

        protected override string InternalUniqueId
        {
            get => UserSessionId;
            set => UserSessionId = value;
        }

        protected override StreamVideoCallParticipant Self => this;

        protected override Task UploadCustomDataAsync()
            => Client.SetParticipantCustomDataAsync(this, InternalCustomData.InternalDictionary);

        private const string VideoRotationAngleKey = "videoRotationAngle";

        #region Tracks

        private StreamAudioTrack _audioTrack;
        private StreamVideoTrack _videoTrack;
        private StreamVideoTrack _screenShareTrack;
        private StreamAudioTrack _screenShareAudioTrack;

        #endregion

        #region Sfu State

        private readonly List<TrackType> _publishedTracks = new List<TrackType>();
        private readonly List<string> _roles = new List<string>();

        #endregion

        private BaseStreamTrack GetStreamTrack(TrackType type)
        {
            switch (type)
            {
                case TrackType.Unspecified:
                    throw new NotSupportedException();
                case TrackType.Audio: return _audioTrack;
                case TrackType.Video: return _videoTrack;
                case TrackType.ScreenShare: return _screenShareTrack;
                case TrackType.ScreenShareAudio: return _screenShareAudioTrack;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void UploadLocalParticipantPublishedVideoRotationAngle()
        {
            if (Client.InternalLowLevelClient.RtcSession == null ||
                Client.InternalLowLevelClient.RtcSession.VideoInput == null)
            {
                return;
            }

            if (!IsLocalParticipant)
            {
                return;
            }

            // STreamTODO: REVERT -> DONT COMMIT THIS CHANGE
            return;

            var angle = Client.InternalLowLevelClient.RtcSession.VideoInput.videoRotationAngle;
            var hasPrevAngle = CustomData.TryGet<int>(VideoRotationAngleKey, out var prevAngle);

            if (!hasPrevAngle || Mathf.Abs(angle - prevAngle) > 0)
            {
                //StreamTodo: there can be potentially multiple video tracks so best to store this by track ID
                CustomData.SetAsync(VideoRotationAngleKey, angle);
            }
        }
    }
}