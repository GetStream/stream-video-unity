using System;
using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Core.Utils;
using Unity.WebRTC;
using UnityEngine;
using Participant = Stream.Video.v1.Sfu.Models.Participant;

namespace StreamVideo.Core.StatefulModels
{
    internal sealed class StreamVideoCallParticipant : StreamStatefulModelBase<StreamVideoCallParticipant>,
        IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>,
        IUpdateableFrom<Participant, StreamVideoCallParticipant>,
        IStreamVideoCallParticipant
    {
        public event ParticipantTrackChangedHandler TrackAdded;

        public bool IsLocalParticipant => UserSessionId == Client.InternalLowLevelClient.RtcSession.SessionId;

        #region Tracks

        public IStreamTrack AudioTrack => _audioTrack;
        public IStreamTrack VideoTrack => _videoTrack;
        public IStreamTrack ScreenShareTrack => _screenShareTrack;

        #endregion

        #region State

        public DateTimeOffset JoinedAt { get; private set; }

        public string Role { get; private set; }

        public IStreamVideoUser User { get; set; }

        public string UserSessionId { get; private set; }

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

        public override string ToString()
            => $"{nameof(StreamVideoCallParticipant)} with User ID: {UserId} & Session ID: {SessionId}";

        //StreamTodo: solve with a generic interface and best to be handled by cache layer
        internal void UpdateFromSfu(Participant dto)
        {
            ((IUpdateableFrom<Participant, StreamVideoCallParticipant>)this).UpdateFromDto(dto, Cache);
        }

        //StreamTodo: perhaps distinguish to UpdateFromSfu interface
        void IUpdateableFrom<Participant, StreamVideoCallParticipant>.UpdateFromDto(Participant dto, ICache cache)
        {
            UserId = dto.UserId;
            SessionId = dto.SessionId;
            _publishedTracks.TryReplaceEnumsFromDtoCollection(dto.PublishedTracks, TrackTypeExt.ToPublicEnum, cache);
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

        internal void Update()
        {
            _audioTrack?.Update();
            _videoTrack?.Update();
            _screenShareTrack?.Update();
        }

        internal void SetTrack(TrackType type, MediaStreamTrack mediaStreamTrack, out IStreamTrack streamTrack)
        {
            Debug.LogWarning($"{GetType()} set track of type {type}");
            switch (type)
            {
                case TrackType.Unspecified:
                    throw new NotSupportedException();
                case TrackType.Audio:
                    streamTrack = _audioTrack = new StreamAudioTrack((AudioStreamTrack)mediaStreamTrack);
                    break;
                case TrackType.Video:
                    streamTrack = _videoTrack = new StreamVideoTrack((VideoStreamTrack)mediaStreamTrack);
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
                Logs.Error($"Failed to get track of type `{type}` for: {this}");
                return;
            }
            
            streamTrack.SetEnabled(enabled);

            //StreamTodo: we should trigger some event that track status changed
        }

        protected override string InternalUniqueId
        {
            get => UserSessionId;
            set => UserSessionId = value;
        }

        protected override StreamVideoCallParticipant Self => this;

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
    }
}