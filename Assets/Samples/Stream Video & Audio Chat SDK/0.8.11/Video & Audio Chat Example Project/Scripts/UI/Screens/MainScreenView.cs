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
        }

        protected override void OnHide()
        {
            UIManager.LocalCameraChanged -= OnLocalCameraChanged;
            
            // Notify child components
            _cameraPanel.NotifyParentHide();
            _microphonePanel.NotifyParentHide();
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
            var length = 4;
            for (var i = 0; i < 10; i++)
            {
                var callId = GenerateShortId(length);
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
                }
                
            }
            
            throw new Exception("Failed to generate a unique call ID");
        }

        public static string GenerateShortId(int length = 8)
        {
            // Some symbols, very close visually, are removed like: (1, l, I) or (O, 0)
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789SBZGUV";
            var random = new System.Random();
    
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}