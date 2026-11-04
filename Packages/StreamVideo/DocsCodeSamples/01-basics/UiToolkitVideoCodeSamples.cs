using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideoDocsCodeSamples._01_basics
{
    /// <summary>
    /// Binds a participant video track to a UI Toolkit Image.
    /// Size the element with USS (absolute fill or flex-grow). Otherwise it sizes itself to the texture
    /// and the layout jumps when the incoming resolution changes.
    /// </summary>
    internal class UiToolkitVideoCodeSamples : MonoBehaviour
    {
        public void BindParticipant(IStreamVideoCallParticipant participant)
        {
            foreach (var track in participant.GetTracks())
            {
                OnParticipantTrackAdded(participant, track);
            }

            // A reconnect creates a new track. Bind again from TrackAdded.
            participant.TrackAdded += OnParticipantTrackAdded;
        }

        private void OnEnable()
        {
            _video = _document.rootVisualElement.Q<Image>("video");
            _video.scaleMode = ScaleMode.ScaleAndCrop;
        }

        private void OnParticipantTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
        {
            if (!(track is StreamVideoTrack videoTrack))
            {
                return;
            }

            videoTrack.SetRenderTarget(_video);
        }

        [SerializeField]
        private UIDocument _document;

        private Image _video;
    }
}
