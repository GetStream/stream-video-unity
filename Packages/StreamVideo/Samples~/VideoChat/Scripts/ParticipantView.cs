using System;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject
{
    public class ParticipantView : MonoBehaviour
    {
        public IStreamVideoCallParticipant Participant { get; private set; }

        public void Init(IStreamVideoCallParticipant participant)
        {
            if (Participant != null)
            {
                throw new NotSupportedException("reusing participant view for new participant is not supported yet");
            }

            Participant = participant ?? throw new ArgumentNullException(nameof(participant));

            foreach (var track in Participant.GetTracks())
            {
                OnParticipantTrackAdded(Participant, track);
            }
            
            Participant.TrackAdded += OnParticipantTrackAdded;

            _name.text = Participant.Name;

            if (Participant.IsLocalParticipant)
            {
                _name.text += " (local)";
            }
            
            _name.text += $"<br>{Participant.SessionId}";
        }

        public void UpdateIsDominantSpeaker(bool isDominantSpeaker)
        {
            var frameColor = isDominantSpeaker ? _dominantSpeakerFrameColor : _defaultSpeakerFrameColor;
            _videoFrame.color = frameColor;
        }

        /// <summary>
        /// Call this for local participant only. We use this because we will not receive the `Participant.TrackAdded` event for the local participant.
        /// So if we want to show stream from a local camera we need to hook it up separately
        /// </summary>
        public void SetLocalCameraSource(WebCamTexture localWebCamTexture)
        {
            _localWebCamTexture = localWebCamTexture;
            
            if (_localParticipantRenderTexture != null)
            {
                // Dispose previous texture 
                _localParticipantRenderTexture.Release();
                _localParticipantRenderTexture = null;
            }
            
            _localParticipantRenderTexture = new RenderTexture(localWebCamTexture.width, localWebCamTexture.height, 0, RenderTextureFormat.Default);
            _localParticipantRenderTexture.Create();

            // we set RenderTexture a a RawImage.texture because the RenderTexture will receive video stream from the local camera
            _video.texture = _localParticipantRenderTexture;
        }

        // Called by Unity Engine
        protected void Update()
        {
            if (_localWebCamTexture != null)
            {
                Graphics.Blit(_localWebCamTexture, _localParticipantRenderTexture);
            }
        }

        // Called by Unity Engine
        protected void OnDestroy()
        {
            if (Participant != null)
            {
                Participant.TrackAdded -= OnParticipantTrackAdded;
            }
        }

        [SerializeField]
        private TMP_Text _name;

        [SerializeField]
        private RawImage _video;
        
        [SerializeField]
        private RawImage _videoFrame;
        
        [SerializeField]
        private Color32 _dominantSpeakerFrameColor;
        
        [SerializeField]
        private Color32 _defaultSpeakerFrameColor;

        private AudioSource _audioSource;
        private RenderTexture _localParticipantRenderTexture;
        private WebCamTexture _localWebCamTexture;

        private void OnParticipantTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
        {
            switch (track)
            {
                case StreamAudioTrack streamAudioTrack:
                    Debug.LogWarning("Audio source received");
                    if (_audioSource != null)
                    {
                        Debug.LogError("Multiple audio track!");
                        return;
                    }

                    _audioSource = gameObject.AddComponent<AudioSource>();
                    streamAudioTrack.SetAudioSourceTarget(_audioSource);
                    break;

                case StreamVideoTrack streamVideoTrack:
                    streamVideoTrack.SetRenderTarget(_video);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(track));
            }
        }
    }
}