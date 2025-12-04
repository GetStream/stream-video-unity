using System;
using System.Net.Http;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Utils;
using UnityEngine;
#if STREAM_DEBUG_ENABLED
using System.Linq;
#endif

namespace StreamVideo.ExampleProject
{
    public class StreamVideoManager : MonoBehaviour
    {
        public event Action<IStreamCall> CallStarted;
        public event Action CallEnded;

        public IStreamVideoClient Client { get; private set; }
        public IStreamCall ActiveCall => _activeCall;

        public void Init()
        {
            _clientConfig = new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug,
            };

            Client = StreamVideoClient.CreateDefaultClient(_clientConfig);
            Client.CallStarted += OnCallStarted;
            Client.CallEnded += OnCallEnded;
        }

        /// <summary>
        /// Check if we can use this call ID to create a new call or is it already taken
        /// </summary>
        public async Task<bool> IsCallIdAvailableToTake(string callId)
        {
            try
            {
                var call = await Client.GetCallAsync(StreamCallType.Default, callId);
                return call == null;
            }
            catch (StreamApiException streamApiException)
            {
                return streamApiException.StatusCode == StreamApiException.NotFoundHttpStatusCode &&
                       streamApiException.Code == StreamApiException.NotFoundStreamCode;
            }
        }

        /// <summary>
        /// Join the Call with a given ID. We can either create it or try to join only.
        /// </summary>
        /// <param name="callId">Call ID</param>
        /// <param name="create">Do we create this call before trying to join</param>
        public async Task JoinAsync(string callId, bool create = true)
        {
            if (string.IsNullOrEmpty(callId))
            {
                throw new Exception($"Call ID is required");
            }

            Debug.Log($"Join call, create: {create}, callId: {callId}");
            await Client.JoinCallAsync(StreamCallType.Default, callId, create, ring: true, notify: false);

            if (_autoEnableMicrophone)
            {
                Client.AudioDeviceManager.SetEnabled(true);
            }

            if (_autoEnableCamera)
            {
                Client.VideoDeviceManager.SetEnabled(true);
            }

            if (_playOnCallStart)
            {
                ToggleMusic();
            }
        }

        public void EndActiveCall()
        {
            if (_activeCall == null)
            {
                throw new InvalidOperationException("Tried to end the call but there is not active call.");
            }

            _activeCall.EndAsync().LogIfFailed();
            ToggleMusic(forceStop: true);
        }

        public void LeaveActiveCall()
        {
            if (_activeCall == null)
            {
                throw new InvalidOperationException("Tried to end the call but there is not active call.");
            }

            _activeCall.LeaveAsync().LogIfFailed();
            ToggleMusic(forceStop: true);
        }

        public void ToggleMusic(bool loop = true, bool forceStop = false)
        {
            if (_musicClip == null)
            {
                throw new ArgumentNullException($"{nameof(_musicClip)} is not assigned.");
            }

            var audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (audioSource.isPlaying || forceStop)
            {
                audioSource.Stop();
                return;
            }

            audioSource.clip = _musicClip;
            audioSource.loop = loop;
            audioSource.Play();
        }

        /// <summary>
        /// Read <see cref="IStreamAudioConfig.EnableDtx"/>
        /// </summary>
        /// <param name="value"></param>
        public void SetAudioDtx(bool value) => _clientConfig.Audio.EnableDtx = value;

        /// <summary>
        /// Read <see cref="IStreamAudioConfig.EnableRed"/>
        /// </summary>
        public void SetAudioREDundancyEncoding(bool value) => _clientConfig.Audio.EnableRed = value;

        protected async void Start()
        {
            var credentials = new AuthCredentials(_apiKey, _userId, _userToken);

            try
            {
                await ConnectToStreamAsync(credentials);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected async void OnDestroy()
        {
            if (Client == null)
            {
                return;
            }

            try
            {
                await Client.DisconnectAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Client.CallStarted -= OnCallStarted;
            Client.CallEnded -= OnCallEnded;
            Client.Dispose();
            Client = null;
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        protected void OnApplicationPause(bool pauseStatus)
        {
            if (Client == null)
            {
                return;
            }

            if (pauseStatus)
            {
                // App is going to background
                Client.PauseAndroidAudioPlayback();
                _wasAudioPublishEnabledOnPause = Client.AudioDeviceManager.IsEnabled;
                _wasVideoPublishEnabledOnPause = Client.VideoDeviceManager.IsEnabled;

                Client.AudioDeviceManager.SetEnabled(false);
                Client.VideoDeviceManager.SetEnabled(false);
            }
            else
            {
                // App is coming to foreground
                Client.ResumeAndroidAudioPlayback();

                if (_wasAudioPublishEnabledOnPause)
                {
                    Client.AudioDeviceManager.SetEnabled(true);
                    _wasAudioPublishEnabledOnPause = false;
                }

                if (_wasVideoPublishEnabledOnPause)
                {
                    Client.VideoDeviceManager.SetEnabled(true);
                    _wasVideoPublishEnabledOnPause = false;
                }
            }
        }
#endif

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

#pragma warning disable CS0414 //Disable warning that _info is unused. It's purpose is to display info box in the Unity Inspector only

        [SerializeField]
        [TextArea]
        private string _info
            = "Get your credentials from https://dashboard.getstream.io/. If you leave the credentials empty then Stream's Demo credentials will be used automatically.";

#pragma warning restore CS0414

        [Header("Authorization Credentials")]
        [SerializeField]
        private string _apiKey = "";

        [SerializeField]
        private string _userId = "";

        [SerializeField]
        private string _userToken = "";

        [Header("Background Music in a call")]
        [SerializeField]
        private AudioClip _musicClip = null;

        [SerializeField]
        private bool _playOnCallStart = false;

        [Header("Auto-enable devices on joining a call")]
        [SerializeField]
        private bool _autoEnableCamera = false;

        [SerializeField]
        private bool _autoEnableMicrophone = false;

        private StreamClientConfig _clientConfig;
        private IStreamCall _activeCall;

        private bool _wasAudioPublishEnabledOnPause;
        private bool _wasVideoPublishEnabledOnPause;

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

            await Client.ConnectUserAsync(credentials);
        }

        /// <summary>
        /// This method will fetch Stream's demo credentials. These credentials are not usable in a real production due to limited rates.
        /// Customer accounts do have a FREE tier so please register at https://getstream.io/ to get your own app ID and credentials.
        /// </summary>
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
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            CallStarted?.Invoke(call);
        }

        private void OnCallEnded(IStreamCall call)
        {
#if STREAM_DEBUG_ENABLED
            try
            {
                if (_activeCall == null)
                {
                    Debug.LogError("Active call was null when trying to end it. call is null " + (call == null));
                    return;
                }

                if (_activeCall.Participants == null)
                {
                    Debug.LogError("Active call participants were null when trying to end it. call is null " +
                                   (call == null));
                    return;
                }

                var callId = _activeCall.Id;
                var localParticipant = _activeCall.Participants.First(p => p.IsLocalParticipant);
                Client.SendDebugLogs(call.Id, localParticipant.SessionId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
#endif

            _activeCall = null;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            CallEnded?.Invoke();
        }
    }
}