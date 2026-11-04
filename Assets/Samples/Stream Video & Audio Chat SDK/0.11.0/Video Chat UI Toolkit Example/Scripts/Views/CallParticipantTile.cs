using System;
using System.Collections.Generic;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using UnityEngine;
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

        public void Update(bool highlightSpeaker)
        {
            if (Participant.IsLocalParticipant)
            {
                var client = _app.Client;
                // Preview is already upright. It stays 16x16 until the camera delivers a frame.
                var preview = client.VideoDeviceManager.GetLocalPreviewTexture();
                var hasFrame = preview != null && preview.width > 16 && preview.height > 16;
                _view.SetVideo(hasFrame ? preview : null, client.VideoDeviceManager.IsEnabled);
            }
            else
            {
                _view.SetTrackVideo(Participant.IsVideoEnabled);
                UpdateRequestedResolution();
            }

            _view.SetName(GetDisplayName(Participant));
            _view.SetAudio(Participant.IsAudioEnabled, Participant.IsSpeaking);
            _view.SetConnectionQuality(Participant.ConnectionQuality);
            _view.SetHighlighted(highlightSpeaker && Participant.IsDominantSpeaker);
        }

        public void Dispose()
        {
            Participant.TrackAdded -= OnTrackAdded;

            if (_audioSourceHost != null)
            {
                Object.Destroy(_audioSourceHost);
            }

            Root.RemoveFromHierarchy();
        }

        private readonly VideoChatApp _app;
        private readonly ParticipantTileView _view;
        private readonly HashSet<IStreamTrack> _processedTracks = new HashSet<IStreamTrack>();

        private GameObject _audioSourceHost;
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
                    if (Participant.IsLocalParticipant)
                    {
                        return;
                    }

                    // The local tile shows GetLocalPreviewTexture instead. Binding the published track here
                    // would replace that preview, because both write the same Image.
                    videoTrack.SetRenderTarget(_view.VideoImage);
                    break;
            }
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
