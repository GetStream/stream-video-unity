using System;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using TMPro;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject
{
    public class ParticipantView : MonoBehaviour
    {
        public void Init(IStreamVideoCallParticipant participant)
        {
            if (_participant != null)
            {
                throw new NotSupportedException("reusing participant view for new participant is not supported yet");
            }

            _participant = participant ?? throw new ArgumentNullException(nameof(participant));

            foreach (var track in _participant.GetTracks())
            {
                OnParticipantTrackAdded(_participant, track);
            }
            
            _participant.TrackAdded += OnParticipantTrackAdded;

            _name.text = _participant.Name;

            if (_participant.IsLocalParticipant)
            {
                _name.text += " (local)";
            }
        }

        public void OnDestroy()
        {
            if (_participant != null)
            {
                _participant.TrackAdded -= OnParticipantTrackAdded;
            }
        }

        [SerializeField]
        private TMP_Text _name;

        [SerializeField]
        private RawImage _video;

        private IStreamVideoCallParticipant _participant;
        private AudioSource _audioSource;

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