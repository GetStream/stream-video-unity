using System;
using System.Net.Http;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Utils;
using Unity.WebRTC;
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

            //StreamTodo: handle by SDK
            StartCoroutine(WebRTC.Update());
        }

        protected void Update() => _client?.Update();

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

        [Header("The Join Call ID from UI's Input will override this value")]
        [SerializeField]
        private string _joinCallId = "3TK1d0wL2we0";

        [Space(50)]
        [Header("Authorization Credentials")]
        [SerializeField]
        private string _apiKey = "";

        [SerializeField]
        private string _userId = "";

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
            var token = await GetTokenAsync();
            Debug.Log($"Try to get token: {token != null}");

            await _client.ConnectUserAsync(credentials.CreateWithNewUserToken(token));
        }

        //StreamTodo: remove
        private async Task<string> GetTokenAsync()
        {
            var httpClient = new HttpClient();
            var uriBuilder = new UriBuilder()
            {
                Host = "stream-calls-dogfood.vercel.app",
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