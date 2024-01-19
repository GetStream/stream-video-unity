using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.StatefulModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI.Screens
{
    /// <summary>
    /// Screen visible during the active call. Shows other participants
    /// </summary>
    public class CallScreenView : BaseScreenView<CallScreenView.InitArgs>
    {
        /// <summary>
        /// Arguments required to initialize this screen when showing
        /// </summary>
        public readonly struct InitArgs
        {
            public readonly IStreamCall ActiveCall;

            public InitArgs(IStreamCall activeCall)
            {
                ActiveCall = activeCall;
            }
        }

        // Store participant views by SessionID so we can easily find it when they're leaving the call
        private readonly Dictionary<string, ParticipantView> _participantSessionIdToView
            = new Dictionary<string, ParticipantView>();

        [SerializeField]
        private ParticipantView _participantViewPrefab;

        [SerializeField]
        private Transform _dominantSpeakerContainer;

        [SerializeField]
        private Transform _remainingParticipantsContainer;

        [SerializeField]
        private Button _leaveBtn;

        [SerializeField]
        private Button _endBtn;

        [SerializeField]
        private TMP_InputField _joinCallIdInput;

        private IStreamCall _activeCall;
        private ParticipantView _currentDominantSpeakerView;

        protected override void OnShow(InitArgs initArgs)
        {
            _activeCall = initArgs.ActiveCall;

            _leaveBtn.onClick.AddListener(VideoManager.LeaveActiveCall);
            _endBtn.onClick.AddListener(VideoManager.EndActiveCall);

            // If local user is the call owner we can "end" the call for all participants, otherwise we can only "leave" the call
            _endBtn.gameObject.SetActive(_activeCall.IsLocalUserOwner);

            // Generate participant UI for already present participants
            foreach (var participant in _activeCall.Participants)
            {
                AddParticipant(participant, sortParticipantViews: false);
            }
            
            SortParticipantViews();

            // Subscribe to participants joining or leaving the call
            _activeCall.ParticipantJoined += OnParticipantJoined;
            _activeCall.ParticipantLeft += OnParticipantLeft;

            // Subscribe to the change of the most actively speaking participant
            _activeCall.DominantSpeakerChanged += OnDominantSpeakerChanged;

            _activeCall.SortedParticipantsUpdated += SortParticipantViews;

            UIManager.ActiveCameraChanged += OnActiveCameraChanged;

            // Show active call ID so user can copy it and send others to join
            _joinCallIdInput.text = _activeCall.Id;
        }

        protected override void OnHide()
        {
            _leaveBtn.onClick.RemoveListener(VideoManager.LeaveActiveCall);
            _endBtn.onClick.RemoveListener(VideoManager.EndActiveCall);

            if (_activeCall != null)
            {
                _activeCall.ParticipantJoined -= OnParticipantJoined;
                _activeCall.ParticipantLeft -= OnParticipantLeft;
                _activeCall.DominantSpeakerChanged -= OnDominantSpeakerChanged;
                _activeCall.SortedParticipantsUpdated -= SortParticipantViews;
                _activeCall = null;
            }

            RemoveAllParticipants();

            UIManager.ActiveCameraChanged -= OnActiveCameraChanged;
        }

        private void OnDominantSpeakerChanged(IStreamVideoCallParticipant currentDominantSpeaker,
            IStreamVideoCallParticipant previousDominantSpeaker)
        {
            Debug.Log(
                $"Dominant speaker changed from {currentDominantSpeaker.Name} to {previousDominantSpeaker?.Name}");

            foreach (var participantView in _participantSessionIdToView.Values)
            {
                var isDominantSpeaker = participantView.Participant == currentDominantSpeaker;
                participantView.UpdateIsDominantSpeaker(isDominantSpeaker);
            }
            
            SortParticipantViews();
        }
        
        private void OnParticipantJoined(IStreamVideoCallParticipant participant) => AddParticipant(participant, sortParticipantViews: true);
        
        private void OnParticipantLeft(string sessionId, string userId) => RemoveParticipant(sessionId, userId, sortParticipantViews: true);

        private void AddParticipant(IStreamVideoCallParticipant participant, bool sortParticipantViews)
        {
            var parent = GetParticipantViewParent(participant);
            var view = Instantiate(_participantViewPrefab, parent);
            view.Init(participant);
            _participantSessionIdToView.Add(participant.SessionId, view);

            if (participant.IsLocalParticipant)
            {
                // Set input camera as a video source for local participant - we won't receive OnTrack event for local participant
                view.SetLocalCameraSource(UIManager.ActiveCamera);
            }

            if (sortParticipantViews)
            {
                SortParticipantViews();
            }
        }

        private void RemoveParticipant(string sessionId, string userId, bool sortParticipantViews)
        {
            if (!_participantSessionIdToView.TryGetValue(sessionId, out var view))
            {
                Debug.LogError("Failed to find view for removed participant with sessionId: " + sessionId);
                return;
            }

            _participantSessionIdToView.Remove(sessionId);
            Destroy(view.gameObject);
            
            if (sortParticipantViews)
            {
                SortParticipantViews();
            }
        }

        /// <summary>
        /// Sort participant views based on SortedParticipants property.
        /// This will place dominant participant in large window and the other participants in a scrollable view underneath
        /// </summary>
        private void SortParticipantViews()
        {
            var index = 0;
            foreach (var participantView in _participantSessionIdToView.Values)
            {
                var isDominantSpeaker = participantView.Participant == _activeCall.DominantSpeaker;
                var parent = GetParticipantViewParent(isDominantSpeaker);

                participantView.transform.SetParent(parent);

                if (!isDominantSpeaker)
                {
                    // Set valid order of the view relative to other views. We skip this for dominant speaker because he's under a different parent Transform
                    participantView.transform.SetSiblingIndex(index);
                    index++;
                }
            }
        }

        private Transform GetParticipantViewParent(IStreamVideoCallParticipant participant)
        {
            var isDominantSpeaker = participant == _activeCall.DominantSpeaker;
            return GetParticipantViewParent(isDominantSpeaker);
        }

        private Transform GetParticipantViewParent(bool isDominantSpeaker)
            => isDominantSpeaker ? _dominantSpeakerContainer : _remainingParticipantsContainer;

        private void RemoveAllParticipants()
        {
            foreach (var (sessionId, view) in _participantSessionIdToView)
            {
                Destroy(view.gameObject);
            }

            _participantSessionIdToView.Clear();
        }

        private void OnActiveCameraChanged(WebCamTexture activeCamera)
        {
            // Input Camera changed so let's update the preview for local participant
            var localParticipant
                = _participantSessionIdToView.Values.FirstOrDefault(p => p.Participant.IsLocalParticipant);
            if (localParticipant != null)
            {
                localParticipant.SetLocalCameraSource(activeCamera);
            }
        }
    }
}