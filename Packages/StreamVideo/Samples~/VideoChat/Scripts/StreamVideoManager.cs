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

        /// <summary>
        /// API success response template when using Stream's Demo Credentials
        /// </summary>
        private class DemoCredentialsApiResponse
        {
            public string UserId;
            public string Token;
            public string APIKey;
        }

        /// <summary>
        /// API error response template when using Stream's Demo Credentials
        /// </summary>
        private class DemoCredentialsApiError
        {
            public string Error;
        }

        [SerializeField]
        private UIManager _uiManager;

        [Header("The Join Call ID from UI's Input will override this value")]
        [SerializeField]
        private string _joinCallId = "";

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

        private string TryGetCallId(bool create) => create ? Guid.NewGuid().ToString() : _uiManager.JoinCallId;

        private async void OnJoinClicked(bool create)
        {
            try
            {
                var callId = TryGetCallId(create);
                if (string.IsNullOrEmpty(callId))
                {
                    Debug.LogError($"Failed to get call ID in mode create: {create}.");
                    return;
                }

                _client.SetAudioInputSource(_uiManager.InputAudioSource);
                _client.SetCameraInputSource(_uiManager.InputCameraSource);
                _client.SetCameraInputSource(_uiManager.InputSceneCamera);

                Debug.Log($"Join clicked, create: {create}, callId: {callId}");

                var streamCall
                    = await _client.JoinCallAsync(StreamCallType.Default, callId, create, ring: true, notify: false);
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
            var credentialsEmpty = string.IsNullOrEmpty(credentials.ApiKey) &&
                                   string.IsNullOrEmpty(credentials.UserId) &&
                                   string.IsNullOrEmpty(credentials.UserToken);

            if (credentialsEmpty)
            {
                // If custom credentials are not defined - use Stream's Demo Credentials
                Debug.Log("Authorization credentials were not provided. Using Stream's Demo Credentials.");

                var demoCredentials = await GetStreamDemoTokenAsync();
                credentials = new AuthCredentials(demoCredentials.APIKey, demoCredentials.UserId,
                    demoCredentials.Token);
            }

            await _client.ConnectUserAsync(credentials);
        }

        private static async Task<DemoCredentialsApiResponse> GetStreamDemoTokenAsync()
        {
            var serializer = new NewtonsoftJsonSerializer();
            var httpClient = new HttpClient();
            var uriBuilder = new UriBuilder
            {
                Host = "pronto.getstream.io",
                Path = "/api/auth/create-token",
                Query = $"user_id=DemoUser",
                Scheme = "https",
            };

            var uri = uriBuilder.Uri;
            var response = await httpClient.GetAsync(uri);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var apiError = serializer.Deserialize<DemoCredentialsApiError>(result);
                throw new Exception(
                    $"Failed to get demo credentials. Error status code: `{response.StatusCode}`, Error message: `{apiError.Error}`");
            }

            return serializer.Deserialize<DemoCredentialsApiResponse>(result);
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
            _activeCall.DominantSpeakerChanged += _uiManager.DominantSpeakerChanged;

            _uiManager.SetJoinCallId(call.Id);
            _uiManager.SetActiveCall(call);
        }

        private void OnCallEnded(IStreamCall call)
        {
            _activeCall.ParticipantJoined -= _uiManager.AddParticipant;
            _activeCall.ParticipantLeft -= _uiManager.RemoveParticipant;
            _activeCall.DominantSpeakerChanged -= _uiManager.DominantSpeakerChanged;
            _activeCall = null;

            _uiManager.SetJoinCallId(string.Empty);
            _uiManager.SetActiveCall(null);
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