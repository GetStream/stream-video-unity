using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UIToolkit.Screens;
using StreamVideo.Libs;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit
{
    /// <summary>
    /// Entry point of the UI Toolkit sample. Owns the <see cref="IStreamVideoClient"/> and navigates between the Start Call, Lobby, and Call screens.
    /// </summary>
    public class VideoChatApp : MonoBehaviour
    {
        public IStreamVideoClient Client { get; private set; }

        public bool IsConnected => Client != null && Client.IsConnected;

        public string LocalUserName => Client?.LocalUser?.Id;

        public VideoResolution SenderVideoResolution => new VideoResolution(_senderVideoWidth, _senderVideoHeight);

        public VisualTreeAsset ParticipantTileAsset => _participantTileAsset;

        public float UIScale => _screenAdapter?.Scale ?? 1f;

        public void ShowStartCall()
        {
            _lobbyScreen.Hide();
            _callScreen.Hide();
            _startCallScreen.Show();
        }

        public void ShowLobby(string callId, bool isNewCall, string error = null)
        {
            if (!_devicesDefaultStateApplied)
            {
                _devicesDefaultStateApplied = true;
                Client.VideoDeviceManager.SetEnabled(_enableCameraByDefault);
                Client.AudioDeviceManager.SetEnabled(_enableMicrophoneByDefault);
            }

            _startCallScreen.Hide();
            _callScreen.Hide();
            _lobbyScreen.Show(callId, isNewCall, error);
        }

        /// <summary>
        /// Returns false if the call does not exist
        /// </summary>
        public async Task<bool> CallExistsAsync(string callId)
        {
            try
            {
                var call = await Client.GetCallAsync(CallType, callId);
                return call != null;
            }
            catch (StreamApiException e) when (e.StatusCode == StreamApiException.NotFoundHttpStatusCode &&
                                               e.Code == StreamApiException.NotFoundStreamCode)
            {
                return false;
            }
        }

        /// <summary>
        /// Generate a short call ID that is easy to type on another device and is not taken yet
        /// </summary>
        public async Task<string> CreateCallIdAsync()
        {
            var length = 3;
            var smallSet = true;
            for (var i = 0; i < 10; i++)
            {
                var callId = GenerateShortId(length, smallSet);
                if (!await CallExistsAsync(callId))
                {
                    return callId;
                }

                if (i > 3)
                {
                    length = 6;
                }

                if (i > 5)
                {
                    length = 8;
                    smallSet = false;
                }
            }

            throw new Exception("Failed to generate a unique call ID");
        }

        public async Task JoinCallAsync(string callId, bool create)
        {
            _joinCancellation?.Cancel();
            _joinCancellation = new CancellationTokenSource();
            var cancellationToken = _joinCancellation.Token;

            _startCallScreen.Hide();
            _lobbyScreen.Hide();
            _callScreen.ShowJoining(callId);

            try
            {
                // The call screen is bound in OnCallStarted which is invoked before this call returns
                await Client.JoinCallAsync(CallType, callId, create, ring: false, notify: false, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ShowStartCall();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (!cancellationToken.IsCancellationRequested)
                {
                    ShowLobby(callId, create, "Could not join the call. Try again.");
                }
            }
        }

        public async void LeaveCall()
        {
            if (Client.ActiveCall == null)
            {
                // Still joining. Cancelling the join brings the user back to the Start Call screen
                _joinCancellation?.Cancel();
                ShowStartCall();
                return;
            }

            try
            {
                await Client.ActiveCall.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (_callScreen.IsVisible)
                {
                    ShowStartCall();
                }
            }
        }

        /// <summary>
        /// Switch between front and back camera on mobile, or to the next available camera on desktop
        /// </summary>
        public void SwitchCamera()
        {
            var videoDeviceManager = Client.VideoDeviceManager;
            var devices = videoDeviceManager.EnumerateDevices().ToList();
            if (devices.Count < 2)
            {
                return;
            }

            var current = videoDeviceManager.SelectedDevice;
            var currentIndex = devices.IndexOf(current);

            var next = devices[(currentIndex + 1) % devices.Count];
            if (currentIndex >= 0 && Application.isMobilePlatform)
            {
                var opposite = devices.FirstOrDefault(d => d.IsFrontFacing != current.IsFrontFacing);
                if (opposite != default)
                {
                    next = opposite;
                }
            }

            SelectCamera(next);
        }

        /// <summary>
        /// Select the camera using the configured sender resolution and FPS. The camera enabled state is preserved.
        /// </summary>
        public void SelectCamera(CameraDeviceInfo device)
        {
            var videoDeviceManager = Client.VideoDeviceManager;
            videoDeviceManager.SelectDevice(device, SenderVideoResolution, videoDeviceManager.IsEnabled,
                _senderVideoFps);
        }

        public bool CanSwitchCamera() => Client.VideoDeviceManager.EnumerateDevices().Skip(1).Any();

        protected void Awake()
        {
            Client = StreamVideoClient.CreateDefaultClient(new StreamClientConfig
            {
                LogLevel = StreamLogLevel.All,
            });

            Client.CallStarted += OnCallStarted;
            Client.CallEnded += OnCallEnded;

            // Use a runtime copy so adjusting the UI scale does not modify the PanelSettings asset
            _uiDocument.panelSettings = Instantiate(_uiDocument.panelSettings);
        }

        protected async void Start()
        {
            var root = _uiDocument.rootVisualElement;
            _screenAdapter = new ScreenAdapter(_uiDocument.panelSettings, root.Q("app-root"), root.Q("safe-area"));

            _startCallScreen = new StartCallScreen(this, root.Q("start-call"));
            _lobbyScreen = new LobbyScreen(this, root.Q("lobby"));
            _callScreen = new CallScreen(this, root.Q("call"));

            ShowStartCall();

            new DeviceSelector(this, Client, _senderVideoFps).RequestPermissionsAndSelectDevices();

            try
            {
                await ConnectAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected void Update()
        {
            _screenAdapter?.Update();

            _startCallScreen?.Update();
            _lobbyScreen?.Update();
            _callScreen?.Update();
        }

        protected async void OnDestroy()
        {
            _joinCancellation?.Cancel();

            _callScreen?.Hide();
            _lobbyScreen?.Hide();

            if (Client == null)
            {
                return;
            }

            Client.CallStarted -= OnCallStarted;
            Client.CallEnded -= OnCallEnded;

            var client = Client;
            Client = null;

            try
            {
                await client.DisconnectAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            client.Dispose();
        }

#pragma warning disable CS0414 // Displayed in the Unity Inspector only

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

        [Header("UI")]
        [SerializeField]
        private UIDocument _uiDocument;

        [SerializeField]
        private VisualTreeAsset _participantTileAsset;

        [Header("Devices")]
        [SerializeField]
        private bool _enableCameraByDefault = true;

        [SerializeField]
        private bool _enableMicrophoneByDefault = true;

        [SerializeField]
        private int _senderVideoWidth = 1280;

        [SerializeField]
        private int _senderVideoHeight = 720;

        [SerializeField]
        private int _senderVideoFps = 30;

        private ScreenAdapter _screenAdapter;
        private StartCallScreen _startCallScreen;
        private LobbyScreen _lobbyScreen;
        private CallScreen _callScreen;
        private CancellationTokenSource _joinCancellation;
        private bool _devicesDefaultStateApplied;

        private StreamCallType CallType => _environment == StreamEnvironment.Pronto
            ? StreamCallType.Custom("default-no-recording")
            : StreamCallType.Default;

        private async Task ConnectAsync()
        {
            var credentials = new AuthCredentials(_apiKey, _userId, _userToken);

            var credentialsEmpty = string.IsNullOrEmpty(credentials.ApiKey) &&
                                   string.IsNullOrEmpty(credentials.UserId) &&
                                   string.IsNullOrEmpty(credentials.UserToken);

            if (credentialsEmpty)
            {
                Debug.Log("Authorization credentials were not provided. Using Stream's Demo Credentials.");

                var provider = new StreamDependenciesFactory().CreateDemoCredentialsProvider();
                credentials = await provider.GetDemoCredentialsAsync("DemoUser", _environment);
            }

            await Client.ConnectUserAsync(credentials);
        }

        private void OnCallStarted(IStreamCall call)
        {
            if (_joinCancellation != null && _joinCancellation.IsCancellationRequested)
            {
                // The user hung up while the join was still in progress
                call.LeaveAsync().LogIfFailed();
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            _callScreen.Bind(call);
        }

        private void OnCallEnded(IStreamCall call)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            if (_callScreen.IsVisible)
            {
                ShowStartCall();
            }
        }

        private static string GenerateShortId(int length, bool smallSet)
        {
            // Some visually similar symbols are removed, like: (1, l, I) or (O, 0)
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            const string charsSmallSet = "abcdefghjkmnpqrstuvwxyz23456789";

            var symbols = smallSet ? charsSmallSet : chars;
            var random = new System.Random();

            return new string(Enumerable.Repeat(symbols, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
