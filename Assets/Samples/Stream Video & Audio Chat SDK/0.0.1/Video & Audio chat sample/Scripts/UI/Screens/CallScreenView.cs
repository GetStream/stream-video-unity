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
        
        private readonly Dictionary<string, ParticipantView> _participantSessionIdToView
            = new Dictionary<string, ParticipantView>();
        
        [SerializeField]
        private ParticipantView _participantViewPrefab;
        
        [SerializeField]
        private Transform _dominantSpeakerContainer;

        [SerializeField]
        private Transform _remainingParticipantsContainer;
        
        [SerializeField]
        private Transform _participantsContainer;
        
        [SerializeField]
        private Button _leaveBtn;

        [SerializeField]
        private Button _endBtn;
        
        [SerializeField]
        private TMP_InputField _joinCallIdInput;

        private IStreamCall _activeCall;

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
                AddParticipant(participant);
            }

            // Subscribe to participants joining or leaving the call
            _activeCall.ParticipantJoined += AddParticipant;
            _activeCall.ParticipantLeft += RemoveParticipant;

            // Subscribe to the change of the most actively speaking participant
            _activeCall.DominantSpeakerChanged += OnDominantSpeakerChanged;
            
            UIManager.ActiveCameraChanged += OnActiveCameraChanged;
        }

        protected override void OnHide()
        {
            _leaveBtn.onClick.RemoveListener(VideoManager.LeaveActiveCall);
            _endBtn.onClick.RemoveListener(VideoManager.EndActiveCall);

            if (_activeCall != null)
            {
                _activeCall.ParticipantJoined -= AddParticipant;
                _activeCall.ParticipantLeft -= RemoveParticipant;
                _activeCall.DominantSpeakerChanged -= OnDominantSpeakerChanged;
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
        }
        
        private void AddParticipant(IStreamVideoCallParticipant participant)
        {
            var view = Instantiate(_participantViewPrefab, _participantsContainer);
            view.Init(participant);
            _participantSessionIdToView.Add(participant.SessionId, view);

            if (participant.IsLocalParticipant)
            {
                // Set input camera as a video source for local participant - we won't receive OnTrack event for local participant
                view.SetLocalCameraSource(UIManager.ActiveCamera);
            }
        }

        private void RemoveParticipant(string sessionId, string userId)
        {
            if (!_participantSessionIdToView.TryGetValue(sessionId, out var view))
            {
                Debug.LogError("Failed to find view for removed participant with sessionId: " + sessionId);
                return;
            }

            _participantSessionIdToView.Remove(sessionId);
            Destroy(view.gameObject);
        }
        
        private void SetJoinCallId(string joinCallId) => _joinCallIdInput.text = joinCallId;
        
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