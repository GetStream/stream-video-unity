using System;
using System.Net.Http;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Utils;
using UnityEngine;

namespace StreamVideo.ExampleProject
{
    public class StreamVideoManager : MonoBehaviour
    {
        protected void Awake()
        {
            _uiManager.JoinClicked += OnJoinClicked;
            _uiManager.LeaveCallClicked += OnLeaveCallClicked;
            _uiManager.EndCallClicked += OnEndCallClicked;
            _uiManager.ToggledAudioRed += OnToggledAudioRed;
            _uiManager.ToggledAudioDtx += OnToggledAudioDtx;
        }

        protected void Start()
        {
            if (!string.IsNullOrEmpty(_joinCallId))
            {
                _uiManager.SetJoinCallId(_joinCallId);
            }

            var credentials = new AuthCredentials(_apiKey, _userId, _userToken);

            _clientConfig = new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug,
                Audio =
                {
                    EnableRed = _uiManager.AudioRedEnabled,
                    EnableDtx = _uiManager.AudioDtxEnabled
                }
            };

            _client = StreamVideoClient.CreateDefaultClient(_clientConfig);
            _client.CallStarted += OnCallStarted;
            _client.CallEnded += OnCallEnded;

            ConnectToStreamAsync(credentials).LogIfFailed();
        }

        protected async void OnDestroy()
        {
            _uiManager.JoinClicked -= OnJoinClicked;
            _uiManager.LeaveCallClicked -= OnLeaveCallClicked;
            _uiManager.EndCallClicked -= OnEndCallClicked;
            _uiManager.ToggledAudioRed -= OnToggledAudioRed;
            _uiManager.ToggledAudioDtx -= OnToggledAudioDtx;

            if (_client != null)
            {
                try
                {
                    await _client.DisconnectAsync();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                _client.CallStarted -= OnCallStarted;
                _client.CallEnded -= OnCallEnded;
                _client.Dispose();
                _client = null;
            }
        }
        
        private class TokenResponse
        {
            public string userId;
            public string token;
        }

        [SerializeField]
        private UIManager _uiManager;

        [Header("The Join Call ID from UI Input will override this value")]
        [SerializeField]
        private string _joinCallId = "";

        [Space(50)]
        [Header("Authorization Credentials")]
        [Header("You can find the API KEY in Stream Dashboard")]
        [SerializeField]
        private string _apiKey = "";

        [SerializeField]
        private string _userId = "";

        [Header("For testing - you can use token generator on our website")]
        [Header("For production - generate tokens with your backend using your Stream App Secret")]
        [SerializeField]
        private string _userToken = "";

        private IStreamVideoClient _client;
        private StreamClientConfig _clientConfig;
        private IStreamCall _activeCall;

        private string GetOrCreateCallId(bool create) => create ? Guid.NewGuid().ToString().Replace("-", "") : _uiManager.JoinCallId;

        private async void OnJoinClicked(bool create)
        {
            try
            {
                var callId = GetOrCreateCallId(create);
                if (string.IsNullOrEmpty(callId))
                {
                    Debug.LogError($"Failed to get call ID in mode create: {create}.");
                    return;
                }

                _client.SetAudioInputSource(_uiManager.InputAudioSource);
                _client.SetCameraInputSource(_uiManager.InputCameraSource);
                _client.SetCameraInputSource(_uiManager.InputSceneCamera);

                Debug.Log($"Join clicked, create: {create}, callId: {callId}");

                var streamCall = await _client.JoinCallAsync(StreamCallType.Default, callId, create, ring: true, notify: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        private void OnToggledAudioDtx(bool value)
        {
            _clientConfig.Audio.EnableDtx = value;
        }

        private void OnToggledAudioRed(bool value)
        {
            _clientConfig.Audio.EnableRed = value;
        }
        
        private async Task ConnectToStreamAsync(AuthCredentials credentials)
        {
            var credentialsEmpty = string.IsNullOrEmpty(credentials.ApiKey) && string.IsNullOrEmpty(credentials.UserId);
            if (credentialsEmpty)
            {
                // If user didn't provide credentials - use the demo credentials
                _apiKey = "hd8szvscpxvd";
                _userId = "daniel_sierpinski";
                
                // Request demo user token
                var token = await GetTokenAsync();
                
                // Create demo credentials
                credentials = new AuthCredentials(_apiKey, _userId, token);
            }

            await _client.ConnectUserAsync(credentials);
        }

        /// <summary>
        /// Example of how to get authentication token from the backend endpoint.
        /// This is a demo token provider with short-lived token with limited privileges.
        /// In your own project, you should setup an endpoint that will authorize users and generate tokens for them using your own app secret key that you get from the Stream's Dashboard
        /// </summary>
        private async Task<string> GetTokenAsync()
        {
            var httpClient = new HttpClient();
            var uriBuilder = new UriBuilder()
            {
                Host = "pronto.getstream.io",
                Path = "/api/auth/create-token",
                Query = $"api_key={_apiKey}&user_id={_userId}&exp=14400",
                Scheme = "https",
            };

            var uri = uriBuilder.Uri;
            var response = await httpClient.GetAsync(uri);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get token with status code: {response.StatusCode}");
            }

            var serializer = new NewtonsoftJsonSerializer();
            var tokenResponse = serializer.Deserialize<TokenResponse>(result);

            return tokenResponse.token;
        }
        
        private void OnCallEnded(IStreamCall call)
        {
            _activeCall.ParticipantJoined -= _uiManager.AddParticipant;
            _activeCall.ParticipantLeft -= _uiManager.RemoveParticipant;
            _activeCall = null; 
            
            _uiManager.SetJoinCallId(string.Empty);
            _uiManager.SetActiveCall(null);
        }

        private void OnCallStarted(IStreamCall call)
        {
            _activeCall = call;
            foreach (var participant in _activeCall.Participants)
            {
                _uiManager.AddParticipant(participant);
            }
                
            _activeCall.ParticipantJoined += _uiManager.AddParticipant;
            _activeCall.ParticipantLeft += _uiManager.RemoveParticipant;
            
            _uiManager.SetJoinCallId(call.Id);
            _uiManager.SetActiveCall(call);
        }
        
        private void OnEndCallClicked()
        {
            _activeCall.EndAsync().LogIfFailed();
        }

        private void OnLeaveCallClicked()
        {
            _activeCall.LeaveAsync().LogIfFailed();
        }
    }
}