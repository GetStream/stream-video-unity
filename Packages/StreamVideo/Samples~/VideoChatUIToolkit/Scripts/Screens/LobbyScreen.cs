using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.ExampleProject.UIToolkit.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Screens
{
    /// <summary>
    /// Shown before starting or joining a call. Displays the local camera preview and lets the user toggle the microphone and camera.
    /// Desktop and the Editor also show device lists. A mobile player does not. The device state carries over into the call.
    /// </summary>
    internal sealed class LobbyScreen : ScreenBase
    {
        public LobbyScreen(VideoChatApp app, VisualElement root)
            : base(app, root)
        {
            _header = new AccountHeaderView(root.Q("header"), showCloseButton: true);
            _header.CloseClicked += App.ShowStartCall;

            _body = root.Q("body");
            _main = root.Q("main");
            _previewArea = root.Q("preview-area");
            _preview = root.Q("preview");
            _side = root.Q("side");
            _portraitIntro = root.Q(className: "lobby__intro--portrait");
            _titlePortrait = root.Q<Label>("title-portrait");
            _titleLandscape = root.Q<Label>("title-landscape");
            _errorLabel = root.Q<Label>("error-label");
            _joinButton = root.Q<Button>("join-button");

            _previewView = new ParticipantTileView(_preview);
            _previewView.SetHighlighted(true);

            _micButton = root.Q<Button>("mic-button");
            _micIcon = root.Q("mic-icon");
            _cameraButton = root.Q<Button>("camera-button");
            _cameraIcon = root.Q("camera-icon");

            _micButton.clicked += () => App.Client.AudioDeviceManager.SetEnabled(!App.Client.AudioDeviceManager.IsEnabled);
            _cameraButton.clicked += () => App.Client.VideoDeviceManager.SetEnabled(!App.Client.VideoDeviceManager.IsEnabled);
            _joinButton.clicked += OnJoinClicked;

            _microphoneDropdown = root.Q<DropdownField>("microphone-dropdown");
            _cameraDropdown = root.Q<DropdownField>("camera-dropdown");

            _microphoneDropdown.RegisterValueChangedCallback(_ => OnMicrophonePicked());
            _cameraDropdown.RegisterValueChangedCallback(_ => OnCameraPicked());

            // UnityEngine.Application stays false in the Editor, including the Device Simulator.
            // Device.Application would hide the lists while simulating a phone.
            root.Q(className: "lobby__devices").EnableInClassList(HiddenClass, Application.isMobilePlatform);
        }

        public void Show(string callId, bool isNewCall, string error)
        {
            _callId = callId;
            _isNewCall = isNewCall;

            var title = isNewCall ? "Set up your call" : "Set up your call\nbefore joining";
            _titlePortrait.text = title;
            _titleLandscape.text = title;
            _joinButton.text = isNewCall ? "Start Call" : "Join Call";

            _errorLabel.text = error ?? string.Empty;
            _errorLabel.EnableInClassList(HiddenClass, string.IsNullOrEmpty(error));

            if (!IsVisible)
            {
                App.Client.AudioDeviceManager.SelectedDeviceChanged += OnMicrophoneChanged;
                App.Client.VideoDeviceManager.SelectedDeviceChanged += OnCameraChanged;
            }

            RefreshMicrophones();
            RefreshCameras();

            SetVisible();
        }

        protected override void OnHide()
        {
            App.Client.AudioDeviceManager.SelectedDeviceChanged -= OnMicrophoneChanged;
            App.Client.VideoDeviceManager.SelectedDeviceChanged -= OnCameraChanged;
        }

        protected override void OnUpdate()
        {
            var client = App.Client;
            var userName = App.LocalUserName;
            _header.SetUser(userName);

            var isCameraEnabled = client.VideoDeviceManager.IsEnabled;
            var isMicEnabled = client.AudioDeviceManager.IsEnabled;
            CallControls.UpdateDeviceButton(_cameraButton, _cameraIcon, isCameraEnabled, "icon--videocam", "icon--videocam-off");
            CallControls.UpdateDeviceButton(_micButton, _micIcon, isMicEnabled, "icon--mic", "icon--mic-off");

            var webCamTexture = client.VideoDeviceManager.GetSelectedDeviceWebCamTexture();
            var isCapturing = webCamTexture != null && webCamTexture.isPlaying && webCamTexture.width > 16;
            var rotation = isCapturing ? webCamTexture.videoRotationAngle : 0;
            _previewView.SetName(userName);
            _previewView.SetVideo(isCapturing ? webCamTexture : null, rotation, isCameraEnabled);
            _previewView.SetAudio(isMicEnabled, isSpeaking: false);

            UpdatePreviewSize();
        }

        // Height to width ratio of the camera preview
        private const float PreviewAspect = 0.72f;
        private const float MinPreviewHeight = 140;

        private readonly AccountHeaderView _header;
        private readonly VisualElement _body;
        private readonly VisualElement _main;
        private readonly VisualElement _previewArea;
        private readonly VisualElement _preview;
        private readonly VisualElement _side;
        private readonly VisualElement _portraitIntro;
        private readonly Label _titlePortrait;
        private readonly Label _titleLandscape;
        private readonly Label _errorLabel;
        private readonly Button _joinButton;
        private readonly Button _micButton;
        private readonly VisualElement _micIcon;
        private readonly Button _cameraButton;
        private readonly VisualElement _cameraIcon;
        private readonly ParticipantTileView _previewView;
        private readonly DropdownField _microphoneDropdown;
        private readonly DropdownField _cameraDropdown;
        private readonly List<MicrophoneDeviceInfo> _microphones = new List<MicrophoneDeviceInfo>();
        private readonly List<CameraDeviceInfo> _cameras = new List<CameraDeviceInfo>();

        private string _callId;
        private bool _isNewCall;
        private Vector2 _lastPreviewSize;

        private void OnJoinClicked()
        {
            if (!App.IsConnected)
            {
                return;
            }

            App.JoinCallAsync(_callId, create: _isNewCall);
        }

        private void OnMicrophoneChanged(MicrophoneDeviceInfo previousDevice, MicrophoneDeviceInfo currentDevice)
            => RefreshMicrophones();

        private void OnCameraChanged(CameraDeviceInfo previousDevice, CameraDeviceInfo currentDevice)
            => RefreshCameras();

        private void OnMicrophonePicked()
        {
            var index = _microphoneDropdown.index;
            if (index < 0 || index >= _microphones.Count)
            {
                return;
            }

            var audioDeviceManager = App.Client.AudioDeviceManager;
            audioDeviceManager.SelectDevice(_microphones[index], audioDeviceManager.IsEnabled);
        }

        private void OnCameraPicked()
        {
            var index = _cameraDropdown.index;
            if (index < 0 || index >= _cameras.Count)
            {
                return;
            }

            App.SelectCamera(_cameras[index]);
        }

        private void RefreshMicrophones()
        {
            var audioDeviceManager = App.Client.AudioDeviceManager;
            _microphones.Clear();
            _microphones.AddRange(audioDeviceManager.EnumerateDevices());
            SetDropdownChoices(_microphoneDropdown, _microphones.Select(d => d.Name),
                _microphones.IndexOf(audioDeviceManager.SelectedDevice));
        }

        private void RefreshCameras()
        {
            var videoDeviceManager = App.Client.VideoDeviceManager;
            _cameras.Clear();
            _cameras.AddRange(videoDeviceManager.EnumerateDevices());
            SetDropdownChoices(_cameraDropdown, _cameras.Select(d => d.Name),
                _cameras.IndexOf(videoDeviceManager.SelectedDevice));
        }

        /// <summary>
        /// Duplicate device names get a numeric suffix because the dropdown resolves the selected index by value
        /// </summary>
        private static void SetDropdownChoices(DropdownField dropdown, IEnumerable<string> names, int selectedIndex)
        {
            var choices = new List<string>();
            foreach (var name in names)
            {
                var choice = name;
                for (var i = 2; choices.Contains(choice); i++)
                {
                    choice = name + " (" + i + ")";
                }

                choices.Add(choice);
            }

            dropdown.choices = choices;
            dropdown.SetValueWithoutNotify(selectedIndex >= 0 ? choices[selectedIndex] : string.Empty);
            dropdown.SetEnabled(choices.Count > 0);
        }

        /// <summary>
        /// Keep the preview aspect ratio. In portrait it spans the content width, in landscape it fits the space left of the controls.
        /// </summary>
        private void UpdatePreviewSize()
        {
            if (Root.panel == null)
            {
                return;
            }

            Vector2 size;
            var isLandscape = _main.resolvedStyle.flexDirection == FlexDirection.Row;
            if (isLandscape)
            {
                var bounds = _previewArea.contentRect.size;
                if (!(bounds.x > 0) || !(bounds.y > 0))
                {
                    return;
                }

                size = bounds.x * PreviewAspect <= bounds.y
                    ? new Vector2(bounds.x, bounds.x * PreviewAspect)
                    : new Vector2(bounds.y / PreviewAspect, bounds.y);
            }
            else
            {
                var width = _main.contentRect.width;
                if (!(width > 0))
                {
                    return;
                }

                var introHeight = _portraitIntro.layout.height + _portraitIntro.resolvedStyle.marginBottom;
                var available = _body.contentRect.height - introHeight - _side.layout.height;
                var height = Mathf.Max(MinPreviewHeight, Mathf.Min(width * PreviewAspect, available));
                size = new Vector2(width, height);
            }

            if ((size - _lastPreviewSize).sqrMagnitude < 0.25f)
            {
                return;
            }

            _lastPreviewSize = size;
            _preview.style.width = size.x;
            _preview.style.height = size.y;
        }
    }
}
