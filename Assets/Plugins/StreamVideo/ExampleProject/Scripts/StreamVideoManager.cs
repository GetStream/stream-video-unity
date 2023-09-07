using System;
using System.Net.Http;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
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
        }

        protected void Start()
        {
            if (!string.IsNullOrEmpty(_joinCallId))
            {
                _uiManager.SetJoinCallId(_joinCallId);
            }

            var credentials = new AuthCredentials(_apiKey, _userId, _userToken);

            _client = StreamVideoClient.CreateDefaultClient(new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug
            });

            ConnectToStreamAsync(credentials).LogIfFailed();

            //StreamTodo: handle by SDK
            StartCoroutine(WebRTC.Update());
        }

        protected void Update() => _client?.Update();

        protected async void OnDestroy()
        {
            _uiManager.JoinClicked -= OnJoinClicked;

            if (_client == null)
            {
                return;
            }

            try
            {
                await _client.DisconnectAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            _client.Dispose();
            _client = null;
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

                _uiManager.SetJoinCallId(callId);

                _client.SetAudioInputSource(_uiManager.InputAudioSource);
                _client.SetCameraInputSource(_uiManager.InputCameraSource);
                _client.SetCameraInputSource(_uiManager.InputSceneCamera);
                

                Debug.Log($"Join clicked, create: {create}, callId: {callId}");

                var streamCall
                    = await _client.JoinCallAsync(StreamCallType.Default, callId, create, ring: true, notify: false);

                foreach (var participant in streamCall.Participants)
                {
                    _uiManager.AddParticipant(participant);
                }
                
                streamCall.ParticipantJoined += _uiManager.AddParticipant;
                streamCall.ParticipantLeft += _uiManager.RemoveParticipant;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
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
    }
}