using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Libs;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Utils;
using UnityEngine;

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
            Client.CallLeaving += OnCallLeaving;
            Client.CallEnded += OnCallEnded;
        }

        /// <summary>
        /// Check if we can use this call ID to create a new call or is it already taken
        /// </summary>
        public async Task<bool> IsCallIdAvailableToTake(string callId)
        {
            try
            {
                var call = await Client.GetCallAsync(CallType, callId);
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
            await Client.JoinCallAsync(CallType, callId, create, ring: true, notify: false);

            if (_autoEnableMicrophone)
            {
                Client.AudioDeviceManager.SetEnabled(true);
            }

            if (_autoEnableCamera)
            {
                if (_delayCameraEnable)
                {
                    // Fire-and-forget: do NOT await here so JoinAsync returns and the call
                    // screen appears while video is still off. This reproduces the customer's
                    // "enable video ~3s after join" flow that triggers a late publisher
                    // renegotiation.
                    Debug.Log($"[DelayedCamera] Scheduling delayed camera enable in {_autoEnableCameraDelaySeconds}s.");
                    DelayedEnableCameraAsync().LogIfFailed();
                }
                else
                {
                    Debug.Log("[DelayedCamera] Enabling camera immediately (delay disabled).");
                    Client.VideoDeviceManager.SetEnabled(true);
                }
            }
            else
            {
                Debug.Log("[DelayedCamera] Auto-enable camera is OFF; camera will not be enabled automatically.");
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

        public void MuteLocally(IStreamVideoCallParticipant participant)
        {
            _isUserMutedLocally[participant.UserId] = true;

            if (participant.AudioTrack != null && participant.AudioTrack is StreamAudioTrack streamAudioTrack)
            {
                streamAudioTrack.MuteLocally();
            }
        }

        public void UnmuteLocally(IStreamVideoCallParticipant participant)
        {
            if (!_isUserMutedLocally.ContainsKey(participant.UserId))
            {
                return;
            }

            _isUserMutedLocally.Remove(participant.UserId);

            if (participant.AudioTrack != null && participant.AudioTrack is StreamAudioTrack streamAudioTrack)
            {
                streamAudioTrack.UnmuteLocally();
            }
        }

        public bool IsParticipantMutedLocally(IStreamVideoCallParticipant participant)
            => _isUserMutedLocally.ContainsKey(participant.UserId) && _isUserMutedLocally[participant.UserId];

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
            Client.CallLeaving -= OnCallLeaving;
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

        [Header("Demo Credentials")]
        [SerializeField]
        private StreamEnvironment _environment = StreamEnvironment.Demo;

        [Header("Background Music in a call")]
        [SerializeField]
        private AudioClip _musicClip = null;

        [SerializeField]
        private bool _playOnCallStart = false;

        [Header("Auto-enable devices on joining a call")]
        [SerializeField]
        private bool _autoEnableCamera = true;

        [Tooltip("When enabled, the camera is enabled after a delay (mimicking a cold-camera warm-up gate) " +
                 "instead of immediately after joining. This reproduces the late publisher renegotiation flow.")]
        [SerializeField]
        private bool _delayCameraEnable = true;

        [Tooltip("Delay (in seconds) before the camera is auto-enabled when " + nameof(_delayCameraEnable) + " is on.")]
        [SerializeField]
        private float _autoEnableCameraDelaySeconds = 8f;

        [SerializeField]
        private bool _autoEnableMicrophone = true;

        private StreamClientConfig _clientConfig;
        private IStreamCall _activeCall;

        private StreamCallType CallType => _environment == StreamEnvironment.Pronto
            ? StreamCallType.Custom("default-no-recording")
            : StreamCallType.Default;

        private bool _wasAudioPublishEnabledOnPause;
        private bool _wasVideoPublishEnabledOnPause;

        // We mute by user ID because Session ID will change every time the user reconnects
        private readonly Dictionary<string, bool> _isUserMutedLocally = new Dictionary<string, bool>();

        private async Task ConnectToStreamAsync(AuthCredentials credentials)
        {
            var credentialsEmpty = string.IsNullOrEmpty(credentials.ApiKey) &&
                                   string.IsNullOrEmpty(credentials.UserId) &&
                                   string.IsNullOrEmpty(credentials.UserToken);

            if (credentialsEmpty)
            {
                Debug.Log("Authorization credentials were not provided. Using Stream's Demo Credentials.");

                var factory = new StreamDependenciesFactory();
                var provider = factory.CreateDemoCredentialsProvider();
                credentials = await provider.GetDemoCredentialsAsync("DemoUser", _environment);
            }

            await Client.ConnectUserAsync(credentials);
        }

        /// <summary>
        /// Enables the camera after <see cref="_autoEnableCameraDelaySeconds"/> to mimic the customer's
        /// cold-camera warm-up gate. Enabling video after join forces a late publisher renegotiation.
        /// </summary>
        private async Task DelayedEnableCameraAsync()
        {
            try
            {
                Debug.Log($"[DelayedCamera] DelayedEnableCameraAsync started. Waiting {_autoEnableCameraDelaySeconds}s...");
                await Task.Delay(TimeSpan.FromSeconds(_autoEnableCameraDelaySeconds));
                Debug.Log("[DelayedCamera] Delay elapsed. Continuing on thread: " +
                          System.Threading.Thread.CurrentThread.ManagedThreadId);

                if (_activeCall == null)
                {
                    Debug.Log("[DelayedCamera] Skipping delayed camera enable - the call is no longer active.");
                    return;
                }

                Debug.Log("[DelayedCamera] Ensuring a working camera is selected before enabling...");
                await EnsureWorkingCameraSelectedAsync();
                Debug.Log("[DelayedCamera] EnsureWorkingCameraSelectedAsync completed. SelectedDevice: " +
                          Client.VideoDeviceManager.SelectedDevice);

                if (_activeCall == null)
                {
                    Debug.Log("[DelayedCamera] Skipping delayed camera enable - the call ended while selecting a camera.");
                    return;
                }

                Debug.Log("[DelayedCamera] Calling VideoDeviceManager.SetEnabled(true). IsEnabled before: " +
                          Client.VideoDeviceManager.IsEnabled);
                Client.VideoDeviceManager.SetEnabled(true);
                Debug.Log("[DelayedCamera] SetEnabled(true) returned. IsEnabled after: " +
                          Client.VideoDeviceManager.IsEnabled);
            }
            catch (Exception e)
            {
                Debug.LogError("[DelayedCamera] DelayedEnableCameraAsync threw an exception:");
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Ensures a working camera is selected (disabled) before enabling video. Mirrors the startup
        /// selection logic in <c>UIManager.SelectFirstWorkingCameraOrDefaultAsync</c>, preferring the
        /// front camera on mobile. Short-circuits if a device is already selected to avoid double-selection.
        /// </summary>
        private async Task EnsureWorkingCameraSelectedAsync()
        {
            // The startup logic in UIManager normally already picked a working/front camera (disabled).
            if (!Client.VideoDeviceManager.SelectedDevice.Equals(default(CameraDeviceInfo)))
            {
                Debug.Log("[DelayedCamera] A device is already selected, short-circuiting selection. SelectedDevice: " +
                          Client.VideoDeviceManager.SelectedDevice);
                return;
            }

            Debug.Log("[DelayedCamera] No device selected yet. Searching for a working camera...");

#if UNITY_ANDROID || UNITY_IOS
            Debug.Log("[DelayedCamera] Mobile platform - preferring front-facing camera.");
            foreach (var device in Client.VideoDeviceManager.EnumerateDevices())
            {
                Debug.Log($"[DelayedCamera] Inspecting device '{device.Name}', IsFrontFacing: {device.IsFrontFacing}.");
                if (!device.IsFrontFacing)
                {
                    continue;
                }

                Debug.Log($"[DelayedCamera] Testing front camera '{device.Name}'...");
                var isWorking = await Client.VideoDeviceManager.TestDeviceAsync(device);
                Debug.Log($"[DelayedCamera] Front camera '{device.Name}' working: {isWorking}.");
                if (isWorking)
                {
                    // Select with enable: false - the subsequent SetEnabled(true) performs the
                    // enable, which exercises the late-renegotiation path.
                    Debug.Log($"[DelayedCamera] Selecting front camera '{device.Name}' (enable: false).");
                    Client.VideoDeviceManager.SelectDevice(device, enable: false);
                    return;
                }
            }
#endif

            Debug.Log("[DelayedCamera] Falling back to TryFindFirstWorkingDeviceAsync...");
            var workingDevice = await Client.VideoDeviceManager.TryFindFirstWorkingDeviceAsync();
            if (workingDevice.HasValue)
            {
                Debug.Log($"[DelayedCamera] Selecting first working device '{workingDevice.Value.Name}' (enable: false).");
                Client.VideoDeviceManager.SelectDevice(workingDevice.Value, enable: false);
                return;
            }

            Debug.LogWarning("[DelayedCamera] No working camera found. Falling back to first device.");

            var firstDevice = Client.VideoDeviceManager.EnumerateDevices().FirstOrDefault();
            if (firstDevice.Equals(default(CameraDeviceInfo)))
            {
                Debug.LogError(
                    "[DelayedCamera] No camera devices found! Video streaming will not work. Please ensure that a camera device is plugged in.");
                return;
            }

            Debug.Log($"[DelayedCamera] Selecting first enumerated device '{firstDevice.Name}' (enable: false).");
            Client.VideoDeviceManager.SelectDevice(firstDevice, enable: false);
        }

        private void OnCallStarted(IStreamCall call)
        {
            _activeCall = call;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            CallStarted?.Invoke(call);
        }

        private void OnCallLeaving(IStreamCall call)
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
                var localParticipant = _activeCall.Participants.FirstOrDefault(p => p.IsLocalParticipant);
                if (localParticipant != null)
                {
                    Client.SendDebugLogs(call.Id, localParticipant.SessionId);
                }
                else
                {
                    Debug.LogWarning("[Debug] Failed to find local participant in active call to send debug stats.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
#endif
        }

        private void OnCallEnded(IStreamCall call)
        {
            _activeCall = null;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            CallEnded?.Invoke();
        }
    }
}