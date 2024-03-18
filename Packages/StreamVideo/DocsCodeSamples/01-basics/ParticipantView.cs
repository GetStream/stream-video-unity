using System;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideoDocsCodeSamples._01_basics
{
    internal class ParticipantView : MonoBehaviour
    {
        // This is how we'll identify that this view belongs to a specific participant
        public string SessionId => _participant.SessionId;
        
        // Call this method to setup view for a participant
        public void Init(IStreamVideoCallParticipant participant)
        {
            if (_participant != null)
            {
                Debug.LogError("Participant view already initialized.");
                return;
            }

            _participant = participant ?? throw new ArgumentNullException(nameof(participant));

            // Handle currently available tracks
            foreach (var track in _participant.GetTracks())
            {
                OnParticipantTrackAdded(_participant, track);
            }

            // Subscribe to event in case new tracks are added
            _participant.TrackAdded += OnParticipantTrackAdded;

            _name.text = _participant.Name;
        }

        public void OnDestroy()
        {
            // It's a good practice to unsubscribe from events when the object is destroyed
            if (_participant != null)
            {
                _participant.TrackAdded -= OnParticipantTrackAdded;
            }
        }

        [SerializeField]
        private Text _name; // This will show participant name

        [SerializeField]
        private RawImage _video; // This will show participant video
        
        [SerializeField]
        private AudioSource _audioSource; // This will play participant audio

        private IStreamVideoCallParticipant _participant;

        private void OnParticipantTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
        {
            switch (track)
            {
                case StreamAudioTrack streamAudioTrack:

                    // Set AudioSource component to be the target of the audio track
                    streamAudioTrack.SetAudioSourceTarget(_audioSource);
                    break;

                case StreamVideoTrack streamVideoTrack:
                    
                    // Set RawImage component to be the target of the video track
                    streamVideoTrack.SetRenderTarget(_video);
                    break;
            }
        }
    }
}