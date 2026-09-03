using System;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.ExampleProject.UI.Devices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI.Screens
{
    /// <summary>
    /// The main screen where a use can create a call or join
    /// </summary>
    public class MainScreenView : BaseScreenView<CallScreenView.ShowArgs>
    {
        /// <summary>
        /// Arguments required to initialize this screen when showing
        /// </summary>
        public readonly struct ShowArgs
        {
        }

        public void Show() => base.Show(new CallScreenView.ShowArgs());

        protected override void OnInit()
        {
            CacheLayout();
            ApplyMainScreenLayout();

            _joinBtn.onClick.AddListener(OnJoinCallButtonClicked);
            _createBtn.onClick.AddListener(OnCreateAndJoinCallButtonClicked);

            _audioRedToggle.onValueChanged.AddListener(VideoManager.SetAudioREDundancyEncoding);
            _audioDtxToggle.onValueChanged.AddListener(VideoManager.SetAudioDtx);
            
            _cameraPanel.Init(VideoManager.Client, UIManager);
            _microphonePanel.Init(VideoManager.Client, UIManager);
        }
        
        protected override void OnShow(CallScreenView.ShowArgs showArgs)
        {
            UIManager.LocalCameraChanged += OnLocalCameraChanged;
            
            // Notify child components
            _cameraPanel.NotifyParentShow();
            _microphonePanel.NotifyParentShow();
            ApplyMainScreenLayout();
        }

        protected override void OnHide()
        {
            UIManager.LocalCameraChanged -= OnLocalCameraChanged;
            
            // Notify child components
            _cameraPanel.NotifyParentHide();
            _microphonePanel.NotifyParentHide();
        }

        protected void Update()
        {
            var size = ((RectTransform)transform).rect.size;
            if (size == _lastSize && Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
            {
                return;
            }

            ApplyMainScreenLayout();
        }

        private void CacheLayout()
        {
            _rootLayout = GetComponent<VerticalLayoutGroup>();
            _header = transform.Find("Header") as RectTransform;
            _top = transform.Find("Top") as RectTransform;
            _bottom = transform.Find("Bottom") as RectTransform;
            _panel = transform.Find("Top/Panel") as RectTransform;
            _beforeCall = transform.Find("Top/Panel/Butttons/BeforeCall") as RectTransform;
            _settings = transform.Find("Bottom/Settings") as RectTransform;
            _localCamera = transform.Find("Bottom/LocalCamera") as RectTransform;

            if (_panel != null)
            {
                _panelLayout = _panel.GetComponent<VerticalLayoutGroup>();
            }

            if (_beforeCall != null)
            {
                _beforeCallLayout = _beforeCall.GetComponent<HorizontalLayoutGroup>();
            }

            if (_bottom != null)
            {
                _bottomHorizontal = _bottom.GetComponent<HorizontalLayoutGroup>();
                _bottomVertical = _bottom.GetComponent<VerticalLayoutGroup>();
                if (_bottomVertical == null)
                {
                    _bottomVertical = _bottom.gameObject.AddComponent<VerticalLayoutGroup>();
                    _bottomVertical.enabled = false;
                }
            }

            if (_localCamera != null)
            {
                _localCameraLayout = _localCamera.GetComponent<HorizontalOrVerticalLayoutGroup>();
            }
        }

        private void ApplyMainScreenLayout()
        {
            var rect = ((RectTransform)transform).rect;
            _lastSize = rect.size;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            var isPortrait = Screen.height >= Screen.width || rect.height >= rect.width;

            const float controlHeight = 72f;
            SetLayout(_joinBtn, preferredHeight: controlHeight, flexibleWidth: 1f);
            SetLayout(_createBtn, preferredHeight: controlHeight, flexibleWidth: 1f);
            SetLayout(_joinCallIdInput, preferredHeight: controlHeight, flexibleWidth: 1f);

            if (_rootLayout != null)
            {
                var pad = isPortrait ? 24 : 100;
                _rootLayout.padding = new RectOffset(pad, pad, pad, pad);
                _rootLayout.spacing = isPortrait ? 16f : 0f;
                _rootLayout.childControlWidth = true;
                _rootLayout.childControlHeight = true;
                _rootLayout.childForceExpandWidth = true;
                _rootLayout.childForceExpandHeight = true;
                _rootLayout.childAlignment = TextAnchor.UpperCenter;
            }

            SetLayout(_header, minHeight: 40f, preferredHeight: 56f, flexibleHeight: 0f);
            SetLayout(_top, flexibleHeight: 0f);
            SetLayout(_bottom, minHeight: 180f, flexibleHeight: 1f, flexibleWidth: 1f);

            if (_panelLayout != null)
            {
                var hPad = isPortrait ? 0 : 300;
                _panelLayout.padding = new RectOffset(hPad, hPad, 20, 0);
                _panelLayout.spacing = isPortrait ? 12f : 0f;
                _panelLayout.childControlWidth = true;
                _panelLayout.childControlHeight = true;
                _panelLayout.childForceExpandWidth = true;
                _panelLayout.childForceExpandHeight = false;
            }

            if (_beforeCallLayout != null)
            {
                _beforeCallLayout.spacing = isPortrait ? 12f : 150f;
                _beforeCallLayout.childControlWidth = true;
                _beforeCallLayout.childControlHeight = true;
                _beforeCallLayout.childForceExpandWidth = true;
                _beforeCallLayout.childForceExpandHeight = false;
            }

            var buttons = _beforeCall != null ? _beforeCall.parent as RectTransform : null;
            SetLayout(buttons, minHeight: controlHeight, preferredHeight: controlHeight, flexibleHeight: 0f);

            ApplyBottomLayout(isPortrait);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        private void ApplyBottomLayout(bool isPortrait)
        {
            if (_bottom == null || _settings == null || _localCamera == null)
            {
                return;
            }

            ConfigureGroup(_bottomHorizontal, enabled: !isPortrait, expandWidth: true, expandHeight: true,
                alignment: TextAnchor.MiddleCenter, spacing: 16f);
            ConfigureGroup(_bottomVertical, enabled: isPortrait, expandWidth: true, expandHeight: true,
                alignment: TextAnchor.MiddleCenter, spacing: 16f);

            if (_localCameraLayout != null)
            {
                _localCameraLayout.childControlWidth = true;
                _localCameraLayout.childControlHeight = true;
                _localCameraLayout.childForceExpandWidth = true;
                _localCameraLayout.childForceExpandHeight = true;
            }

            if (_localCamera.childCount > 0)
            {
                SetLayout(_localCamera.GetChild(0), flexibleWidth: 1f, flexibleHeight: 1f);
            }

            if (isPortrait)
            {
                _localCamera.SetAsFirstSibling();
                SetLayout(_localCamera, minHeight: 160f, preferredWidth: -1f, preferredHeight: -1f, flexibleWidth: 1f,
                    flexibleHeight: 1f);
                SetLayout(_settings, minHeight: 140f, preferredWidth: -1f, preferredHeight: 160f, flexibleWidth: 1f,
                    flexibleHeight: 0f);
            }
            else
            {
                _settings.SetAsFirstSibling();
                SetLayout(_settings, preferredWidth: 500f, preferredHeight: -1f, flexibleWidth: 1f, flexibleHeight: 0f);
                SetLayout(_localCamera, minHeight: 250f, preferredWidth: 250f, preferredHeight: 250f, flexibleWidth: 0f,
                    flexibleHeight: 0f);
            }
        }

        private static void ConfigureGroup(HorizontalOrVerticalLayoutGroup group, bool enabled, bool expandWidth,
            bool expandHeight, TextAnchor alignment, float spacing)
        {
            if (group == null)
            {
                return;
            }

            group.enabled = enabled;
            if (!enabled)
            {
                return;
            }

            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = expandWidth;
            group.childForceExpandHeight = expandHeight;
            group.childAlignment = alignment;
            group.spacing = spacing;
        }

        private static void SetLayout(Component target, float minHeight = -1f, float preferredHeight = -1f,
            float flexibleHeight = -1f, float preferredWidth = -1f, float flexibleWidth = -1f)
        {
            var layoutElement = GetOrAddLayoutElement(target);
            if (layoutElement == null)
            {
                return;
            }

            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = flexibleHeight;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = flexibleWidth;
        }

        private static LayoutElement GetOrAddLayoutElement(Component component)
        {
            if (component == null)
            {
                return null;
            }

            var layoutElement = component.GetComponent<LayoutElement>();
            return layoutElement != null ? layoutElement : component.gameObject.AddComponent<LayoutElement>();
        }

        [SerializeField]
        private Button _joinBtn;

        [SerializeField]
        private Button _createBtn;

        [SerializeField]
        private TMP_InputField _joinCallIdInput;

        [SerializeField]
        private RawImage _localCameraImage;

        [SerializeField]
        private Toggle _audioRedToggle;

        [SerializeField]
        private Toggle _audioDtxToggle;

        [SerializeField]
        private CameraMediaDevicePanel _cameraPanel;

        [SerializeField]
        private MicrophoneMediaDevicePanel _microphonePanel;

        private WebCamDevice _defaultCamera;
        private string _defaultMicrophoneDeviceName;
        private bool _isProcessing;
        private RectTransform _header;
        private RectTransform _top;
        private RectTransform _bottom;
        private RectTransform _panel;
        private RectTransform _beforeCall;
        private RectTransform _settings;
        private RectTransform _localCamera;
        private VerticalLayoutGroup _rootLayout;
        private VerticalLayoutGroup _panelLayout;
        private HorizontalLayoutGroup _beforeCallLayout;
        private HorizontalLayoutGroup _bottomHorizontal;
        private VerticalLayoutGroup _bottomVertical;
        private HorizontalOrVerticalLayoutGroup _localCameraLayout;
        private Vector2 _lastSize;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private async void OnJoinCallButtonClicked()
        {
            try
            {
                if (_isProcessing)
                {
                    return;
                }

                _isProcessing = true;

                if (string.IsNullOrEmpty(_joinCallIdInput.text))
                {
                    Debug.LogError("`Call ID` is required when trying to join a call");
                    return;
                }

                await VideoManager.JoinAsync(_joinCallIdInput.text, create: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async void OnCreateAndJoinCallButtonClicked()
        {
            try
            {
                if (_isProcessing)
                {
                    return;
                }

                _isProcessing = true;
                
                var callId = await CreateRandomCallId();
                await VideoManager.JoinAsync(callId, create: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void OnLocalCameraChanged(WebCamTexture activeCamera)
        {
            _localCameraImage.texture = activeCamera;
        }

        private async Task<string> CreateRandomCallId()
        {
            var length = 3;
            var smallSet = true;
            for (var i = 0; i < 10; i++)
            {
                var callId = GenerateShortId(length, smallSet);
                var isAvailable = await VideoManager.IsCallIdAvailableToTake(callId);
                if (isAvailable)
                {
                    return callId;
                }
                
                #if STREAM_DEBUG_ENABLED
                Debug.LogWarning($"Failed to generate a unique call ID: {callId}, trying again...");
                #endif

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

        public static string GenerateShortId(int length = 8, bool smallSet = false)
        {
            // Some symbols, very close visually, are removed like: (1, l, I) or (O, 0)
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789SBZGUV";
            const string charsSmallSet = "abcdefghjkmnpqrstuvwxyz1234567890";

            var symbols = smallSet ? charsSmallSet : chars;
            var random = new System.Random();
    
            return new string(Enumerable.Repeat(symbols, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
