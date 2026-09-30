using System;
using System.Collections.Generic;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace StreamVideo.ExampleProject.UIToolkit.Views
{
    /// <summary>
    /// Binds a call participant to a <see cref="ParticipantTileView"/>: renders video, plays audio, and shows the participant state.
    /// </summary>
    internal sealed class CallParticipantTile : IDisposable
    {
        public IStreamVideoCallParticipant Participant { get; }
        public VisualElement Root => _view.Root;

        public CallParticipantTile(IStreamVideoCallParticipant participant, VisualElement root, VideoChatApp app)
        {
            Participant = participant ?? throw new ArgumentNullException(nameof(participant));
            _app = app;
            _view = new ParticipantTileView(root);
            _view.SetName(GetDisplayName(participant));

            // Tracks can be received before this tile is created (e.g. while joining the call)
            foreach (var track in Participant.GetTracks())
            {
                HandleTrack(track);
            }

            Participant.TrackAdded += OnTrackAdded;
        }

        public void Update(IStreamCall call, bool highlightSpeaker)
        {
            if (Participant.IsLocalParticipant)
            {
                var client = _app.Client;
                var webCamTexture = client.VideoDeviceManager.GetSelectedDeviceWebCamTexture();
                var rotation = webCamTexture != null && webCamTexture.width > 16 ? webCamTexture.videoRotationAngle : 0;
                _view.SetVideo(call.GetLocalPreviewTexture(), rotation, client.VideoDeviceManager.IsEnabled);
                _view.SetAudio(client.AudioDeviceManager.IsEnabled, Participant.IsSpeaking);
            }
            else
            {
                var texture = _videoTarget != null ? _videoTarget.texture : null;
                var rotation = _videoTrack?.VideoRotationAngle ?? 0;
                _view.SetVideo(texture, rotation, Participant.IsVideoEnabled);
                _view.SetAudio(Participant.IsAudioEnabled, Participant.IsSpeaking);
                UpdateRequestedResolution();
            }

            _view.SetName(GetDisplayName(Participant));
            _view.SetHighlighted(highlightSpeaker && Participant.IsDominantSpeaker);
            _view.SetConnectionQuality(Participant.ConnectionQuality);
        }

        public void Dispose()
        {
            Participant.TrackAdded -= OnTrackAdded;

            if (_audioSourceHost != null)
            {
                Object.Destroy(_audioSourceHost);
            }

            if (_videoTargetHost != null)
            {
                Object.Destroy(_videoTargetHost);
            }

            Root.RemoveFromHierarchy();
        }

        private readonly VideoChatApp _app;
        private readonly ParticipantTileView _view;
        private readonly HashSet<IStreamTrack> _processedTracks = new HashSet<IStreamTrack>();

        private GameObject _audioSourceHost;
        private GameObject _videoTargetHost;
        private RawImage _videoTarget;
        private StreamVideoTrack _videoTrack;
        private Vector2Int _lastRequestedResolution;

        private static string GetDisplayName(IStreamVideoCallParticipant participant)
            => string.IsNullOrEmpty(participant.Name) ? participant.UserId : participant.Name;

        private void OnTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track) => HandleTrack(track);

        private void HandleTrack(IStreamTrack track)
        {
            if (!_processedTracks.Add(track))
            {
                return;
            }

            switch (track)
            {
                case StreamAudioTrack audioTrack:
                    if (Participant.IsLocalParticipant)
                    {
                        return;
                    }

                    // A new track for the same participant can be received after reconnecting
                    if (_audioSourceHost != null)
                    {
                        Object.Destroy(_audioSourceHost);
                    }

                    _audioSourceHost = new GameObject("Audio: " + Participant.UserId);
                    _audioSourceHost.transform.SetParent(_app.transform, worldPositionStays: false);
                    audioTrack.SetAudioSourceTarget(_audioSourceHost.AddComponent<AudioSource>());
                    break;

                case StreamVideoTrack videoTrack:
                    _videoTrack = videoTrack;
                    videoTrack.SetRenderTarget(GetOrCreateVideoTarget());
                    break;
            }
        }

        /// <summary>
        /// <see cref="StreamVideoTrack"/> writes the received texture into a uGUI <see cref="RawImage"/>.
        /// UI Toolkit has no RawImage, so we keep an inactive one as a texture holder and read the texture from it every frame.
        /// </summary>
        private RawImage GetOrCreateVideoTarget()
        {
            if (_videoTarget != null)
            {
                return _videoTarget;
            }

            _videoTargetHost = new GameObject("Video: " + Participant.UserId, typeof(RectTransform));
            _videoTargetHost.SetActive(false);
            _videoTargetHost.transform.SetParent(_app.transform, worldPositionStays: false);
            _videoTarget = _videoTargetHost.AddComponent<RawImage>();
            return _videoTarget;
        }

        /// <summary>
        /// Request the video resolution that matches the rendered size to save bandwidth
        /// </summary>
        private void UpdateRequestedResolution()
        {
            var size = Root.layout.size * _app.UIScale;
            if (!(size.x > 0) || !(size.y > 0))
            {
                return;
            }

            var resolution = new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
            if (resolution == _lastRequestedResolution)
            {
                return;
            }

            _lastRequestedResolution = resolution;
            Participant.UpdateRequestedVideoResolution(new VideoResolution(resolution.x, resolution.y));
        }
    }
}
