using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI
{
    public class UIManager : MonoBehaviour
    {
        public event Action<WebCamTexture> LocalCameraChanged;

        public VideoResolution SenderVideoResolution => new VideoResolution(_senderVideoWidth, _senderVideoHeight);
        public int SenderVideoFps => _senderVideoFps;

        /// <summary>
        /// Join-call ID typed on the main screen. Shared so landscape/portrait copies stay in sync.
        /// </summary>
        public string JoinCallIdDraft
        {
            get => _joinCallIdDraft;
            set => _joinCallIdDraft = value ?? string.Empty;
        }

        public ParticipantView GetOrCreateParticipantView(IStreamVideoCallParticipant participant, Transform parent,
            ParticipantView prefab)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (_participantViews.TryGetValue(participant.SessionId, out var existing) && existing != null)
            {
                AttachParticipantView(existing, parent);
                return existing;
            }

            var view = Instantiate(prefab, parent);
            view.Init(participant, _videoManager);
            _participantViews[participant.SessionId] = view;
            return view;
        }

        public void DestroyParticipantView(string sessionId)
        {
            if (!_participantViews.TryGetValue(sessionId, out var view))
            {
                return;
            }

            _participantViews.Remove(sessionId);
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        protected void Awake()
        {
            _permissionsManager = new PermissionsManager(this);

            _videoManager.Init();

            _videoManager.CallStarted += OnCallStarted;
            _videoManager.CallEnded += OnCallEnded;

            _videoManager.Client.VideoDeviceManager.SelectedDeviceChanged += OnCameraDeviceChanged;
            _videoManager.Client.AudioDeviceManager.SelectedDeviceChanged += OnMicrophoneDeviceChanged;

            CreateParticipantViewsPool();

            _landscapeModeUIScreensSet.Init(_videoManager, uiManager: this);
            _portraitModeUIScreensSet.Init(_videoManager, uiManager: this);

            _isPortrait = IsPortraitMode();
            ConfigureCanvasScaler(_isPortrait);
            SetScreenSetRootsActive(_isPortrait);
            LogUiRotate(
                $"Awake portrait={_isPortrait} forceP={_forceTestPortraitMode} forceL={_forceTestLandscapeMode} " +
                $"{DescribeScreen()} {DescribeCanvas()} active={DescribeScreenSet(ActiveScreenSet)} inactive={DescribeScreenSet(InactiveScreenSet)}");

            if (!_permissionsManager.HasPermission(PermissionsManager.PermissionType.Camera))
            {
                _permissionsManager.RequestPermission(PermissionsManager.PermissionType.Camera,
                    onGranted: () => { SelectFirstWorkingCameraOrDefaultAsync().LogIfFailed(); },
                    onDenied: ()
                        => Debug.LogError("Camera permission was not granted. Video capturing will not work."));
            }
            else
            {
                SelectFirstWorkingCameraOrDefaultAsync().LogIfFailed();
            }

            if (!_permissionsManager.HasPermission(PermissionsManager.PermissionType.Microphone))
            {
                _permissionsManager.RequestPermission(PermissionsManager.PermissionType.Microphone,
                    onGranted: SelectFirstMicrophone,
                    onDenied: ()
                        => Debug.LogError("Microphone permission was not granted. Audio capturing will not work."));
            }
            else
            {
                SelectFirstMicrophone();
            }
        }

        protected void Start() => ShowMainScreen();

        protected void Update()
        {
            var isPortrait = IsPortraitMode();
            if (isPortrait == _isPortrait)
            {
                return;
            }

            _isPortrait = isPortrait;
            ConfigureCanvasScaler(_isPortrait);
            LogUiRotate($"Size changed → portrait={_isPortrait} {DescribeScreen()} {DescribeCanvas()}");
            SwitchScreenSet();
        }

        protected void OnDestroy()
        {
            _isDestroyed = true;

            _videoManager.CallStarted -= OnCallStarted;
            _videoManager.CallEnded -= OnCallEnded;

            if (_videoManager.Client != null)
            {
                _videoManager.Client.VideoDeviceManager.SelectedDeviceChanged -= OnCameraDeviceChanged;
                _videoManager.Client.AudioDeviceManager.SelectedDeviceChanged -= OnMicrophoneDeviceChanged;
            }
        }

        [SerializeField]
        private StreamVideoManager _videoManager;

        [SerializeField]
        private int _senderVideoWidth = 1920;

        [SerializeField]
        private int _senderVideoHeight = 1080;

        [SerializeField]
        private int _senderVideoFps = 30;

        [SerializeField]
        private UIScreensSet _landscapeModeUIScreensSet;

        [SerializeField]
        private UIScreensSet _portraitModeUIScreensSet;

        [SerializeField]
        [Tooltip("Locks portrait UI regardless of Screen size. Works in player builds.")]
        private bool _forceTestPortraitMode;

        [SerializeField]
        [Tooltip("Locks landscape UI regardless of Screen size. Works in player builds. Wins over force portrait.")]
        private bool _forceTestLandscapeMode;

        private readonly Dictionary<string, ParticipantView> _participantViews
            = new Dictionary<string, ParticipantView>();

        private PermissionsManager _permissionsManager;
        private Transform _participantViewsPool;
        private CanvasScaler _canvasScaler;
        private CanvasScaler.ScreenMatchMode _defaultScreenMatchMode;
        private float _defaultMatchWidthOrHeight;
        private bool _isPortrait;
        private bool _isDestroyed;
        private string _joinCallIdDraft = string.Empty;

        private bool CanUseClient
            => !_isDestroyed && _videoManager != null && _videoManager.Client != null;

        private UIScreensSet ActiveScreenSet
            => _isPortrait ? _portraitModeUIScreensSet : _landscapeModeUIScreensSet;

        private UIScreensSet InactiveScreenSet
            => _isPortrait ? _landscapeModeUIScreensSet : _portraitModeUIScreensSet;

        private void OnCallStarted(IStreamCall call) => ShowCallScreen(call);

        private void OnCallEnded()
        {
            ShowMainScreen();
            DestroyAllParticipantViews();
        }

        private void ShowMainScreen()
        {
            LogUiRotate($"ShowMainScreen {DescribeScreenSet(ActiveScreenSet)} {DescribeScreen()}");
            ActiveScreenSet.ShowMainScreen();
        }

        private void ShowCallScreen(IStreamCall call)
        {
            LogUiRotate($"ShowCallScreen {DescribeScreenSet(ActiveScreenSet)} call={call?.Id} {DescribeScreen()}");
            ActiveScreenSet.ShowCallScreen(call);
        }

        private void SwitchScreenSet()
        {
            var outgoing = InactiveScreenSet;
            var incoming = ActiveScreenSet;
            var showCall = _videoManager.ActiveCall != null;

            LogUiRotate(
                $"Switch outgoing={DescribeScreenSet(outgoing)} incoming={DescribeScreenSet(incoming)} " +
                $"showCall={showCall} views={_participantViews.Count} {DescribeScreen()} {DescribeCanvas()}");

            ParkParticipantViews();
            outgoing.HideAll();
            SetScreenSetRootsActive(_isPortrait);

            LogUiRotate(
                $"Switch after SetActive portraitRoot={DescribeScreenSet(_portraitModeUIScreensSet)} " +
                $"landscapeRoot={DescribeScreenSet(_landscapeModeUIScreensSet)}");

            if (showCall)
            {
                incoming.ShowCallScreen(_videoManager.ActiveCall);
            }
            else
            {
                incoming.ShowMainScreen();
            }
        }

        private void SetScreenSetRootsActive(bool isPortrait)
        {
            _portraitModeUIScreensSet.gameObject.SetActive(isPortrait);
            _landscapeModeUIScreensSet.gameObject.SetActive(!isPortrait);
        }

        private void CreateParticipantViewsPool()
        {
            var poolObject = new GameObject("ParticipantViewsPool", typeof(RectTransform), typeof(CanvasGroup));
            poolObject.transform.SetParent(transform, false);

            var rect = (RectTransform)poolObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var canvasGroup = poolObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            _participantViewsPool = poolObject.transform;
        }

        private void ParkParticipantViews()
        {
            foreach (var view in _participantViews.Values)
            {
                if (view != null)
                {
                    AttachParticipantView(view, _participantViewsPool);
                }
            }
        }

        private void DestroyAllParticipantViews()
        {
            foreach (var view in _participantViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _participantViews.Clear();
        }

        private static void AttachParticipantView(ParticipantView view, Transform parent)
        {
            view.transform.SetParent(parent, worldPositionStays: false);
            view.transform.localScale = Vector3.one;
            view.transform.localRotation = Quaternion.identity;
        }

        private void OnMicrophoneDeviceChanged(MicrophoneDeviceInfo previousDevice, MicrophoneDeviceInfo currentDevice)
        {
            Debug.Log($"Changed selected MICROPHONE from `{previousDevice}` to `{currentDevice}`");
        }

        private void OnCameraDeviceChanged(CameraDeviceInfo previousDevice, CameraDeviceInfo currentDevice)
        {
            Debug.Log($"Changed active CAMERA from `{previousDevice}` to `{currentDevice}`");

            if (!CanUseClient)
            {
                return;
            }

            var webCamTexture = _videoManager.Client.VideoDeviceManager.GetSelectedDeviceWebCamTexture();
            LocalCameraChanged?.Invoke(webCamTexture);
        }

        private async Task SelectFirstWorkingCameraOrDefaultAsync()
        {
            if (!CanUseClient || !_videoManager.Client.VideoDeviceManager.EnumerateDevices().Any())
            {
                if (CanUseClient)
                {
                    Debug.LogError(
                        "No camera devices found! Video streaming will not work. Please ensure that a camera device is plugged in.");
                }

                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            foreach (var device in _videoManager.Client.VideoDeviceManager.EnumerateDevices())
            {
                if (!device.IsFrontFacing)
                {
                    continue;
                }

                var isWorking = await _videoManager.Client.VideoDeviceManager.TestDeviceAsync(device);
                if (!CanUseClient)
                {
                    return;
                }

                if (isWorking)
                {
                    _videoManager.Client.VideoDeviceManager.SelectDevice(device, SenderVideoResolution, enable: false, _senderVideoFps);
                    return;
                }
            }
#endif

            var workingDevice = await _videoManager.Client.VideoDeviceManager.TryFindFirstWorkingDeviceAsync();
            if (!CanUseClient)
            {
                return;
            }

            if (workingDevice.HasValue)
            {
                _videoManager.Client.VideoDeviceManager.SelectDevice(workingDevice.Value, SenderVideoResolution, enable: false, _senderVideoFps);
                return;
            }

            Debug.LogWarning("No working camera found. Falling back to first device.");

            var firstDevice = _videoManager.Client.VideoDeviceManager.EnumerateDevices().FirstOrDefault();
            if (firstDevice == default)
            {
                Debug.LogError(
                    "No camera devices found! Video streaming will not work. Please ensure that a camera device is plugged in.");
                return;
            }

            _videoManager.Client.VideoDeviceManager.SelectDevice(firstDevice, SenderVideoResolution, enable: false, _senderVideoFps);
        }

        private void SelectFirstMicrophone()
        {
            // Select first microphone by default
            var microphoneDevice = _videoManager.Client.AudioDeviceManager.EnumerateDevices().FirstOrDefault();
            if (microphoneDevice == default)
            {
                Debug.LogError(
                    "No microphone devices found! Audio streaming will not work. Please ensure that a microphone device is plugged in.");
                return;
            }

            _videoManager.Client.AudioDeviceManager.SelectDevice(microphoneDevice, enable: false);
        }

        private void ConfigureCanvasScaler(bool isPortrait)
        {
            if (_canvasScaler == null)
            {
                _canvasScaler = GetComponent<CanvasScaler>();
                if (_canvasScaler == null)
                {
                    return;
                }

                _defaultScreenMatchMode = _canvasScaler.screenMatchMode;
                _defaultMatchWidthOrHeight = _canvasScaler.matchWidthOrHeight;
            }

            // Landscape CallScreen is a fixed 1080px-tall stack. Expand so 20:9 phones do not
            // scale it off-screen. Portrait keeps the scene scaler so CallScreenPortrait is unchanged.
            if (isPortrait)
            {
                _canvasScaler.screenMatchMode = _defaultScreenMatchMode;
                _canvasScaler.matchWidthOrHeight = _defaultMatchWidthOrHeight;
            }
            else
            {
                _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
        }

        private bool IsPortraitMode()
        {
            if (_forceTestLandscapeMode)
            {
                return false;
            }

            if (_forceTestPortraitMode)
            {
                return true;
            }

            return Screen.height >= Screen.width;
        }

        private static string DescribeScreen()
            => $"{Screen.width}x{Screen.height} {Screen.orientation}";

        private string DescribeCanvas()
        {
            if (_canvasScaler == null)
            {
                return "canvas=null";
            }

            var rect = transform as RectTransform;
            var size = rect != null ? rect.rect : Rect.zero;
            return
                $"canvas={size.width:0}x{size.height:0} scale={_canvasScaler.scaleFactor:0.###} " +
                $"mode={_canvasScaler.screenMatchMode} match={_canvasScaler.matchWidthOrHeight:0.##}";
        }

        private static string DescribeScreenSet(UIScreensSet set)
        {
            if (set == null)
            {
                return "null";
            }

            var go = set.gameObject;
            return $"{go.name} self={go.activeSelf} hier={go.activeInHierarchy}";
        }

        private static void LogUiRotate(string message)
        {
#if STREAM_DEBUG_ENABLED
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "[UIRotate] {0}", message);
#endif
        }
    }
}
