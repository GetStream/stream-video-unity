using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using UnityEngine;

namespace StreamVideoDocsCodeSamples._01_basics
{
    public class VideoManager : MonoBehaviour
    {
        public async Task JoinCallAsync(string callId, StreamCallType callType, bool create, bool ring, bool notify)
        {
            var streamCall = await _client.JoinCallAsync(callType, callId, create, ring, notify);
            
            // Subscribe to events to get notified that streamCall.Participants collection changed
            streamCall.ParticipantJoined += OnParticipantJoined;
            streamCall.ParticipantLeft += OnParticipantLeft;
            
            // Iterate through current participants
            foreach (var participant in streamCall.Participants)
            {
                // Handle participant logic. For example: create a view for e```ach participant
                CreateParticipantView(participant);
            }
        }

        private void OnParticipantLeft(string sessionid, string userid)
        {
            // Try find view for this participant and destroy it because he left the call
            var viewInstance = _participantViews.FirstOrDefault(v => v.SessionId == sessionid);
            if (viewInstance != null)
            {
                // If the participant view was found -> destroy it
                Destroy(viewInstance.gameObject);
            }
        }

        private void OnParticipantJoined(IStreamVideoCallParticipant participant)
        {
            // Create view whenever new participant joins during the call
            CreateParticipantView(participant);
        }

        private void CreateParticipantView(IStreamVideoCallParticipant participant)
        {
            // Create new prefab instance for the view. In this example we'll add it as a child of this gameObject
            var viewInstance = Instantiate(_participantViewPrefab, transform);
            
            // Add to list so we can easily destroy it when a participant leaves the call
            _participantViews.Add(viewInstance);
            
            // Call ParticipantView.Init in order to process the participant tracks and subscribe to events
            viewInstance.Init(participant);
        }

        // Start() is called automatically by UnityEngine
        protected async void Start()
        {
            _client = StreamVideoClient.CreateDefaultClient();

            try
            {
                var authCredentials = new AuthCredentials("api-key", "user-id", "user-token");
                await _client.ConnectUserAsync(authCredentials);
            
                // After we awaited the ConnectUserAsync the client is connected
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        
        [SerializeField]
        private ParticipantView _participantViewPrefab;
        
        private IStreamVideoClient _client;
        private readonly List<ParticipantView> _participantViews = new List<ParticipantView>();
    }
}