using System;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Libs.Auth;
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
            var credentials = new AuthCredentials(_apiKey, _userId, _userToken);

            _client = StreamVideoClient.CreateDefaultClient(new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug
            });

            _client.ConnectUserAsync(credentials).LogIfFailed();


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

        [SerializeField]
        private UIManager _uiManager;

        [Header("This Join ID will override the one from UI input")]
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

        private string TryGetCallId(bool create)
        {
            if (create)
            {
                return Guid.NewGuid().ToString();
            }

            if (!string.IsNullOrEmpty(_joinCallId))
            {
                return _joinCallId;
            }

            if (!string.IsNullOrEmpty(_uiManager.JoinCallId))
            {
                return _uiManager.JoinCallId;
            }

            return null;
        }

        private async void OnJoinClicked(bool create)
        {
            try
            {
                var callId = TryGetCallId(create);
                if (callId == null)
                {
                    Debug.LogError($"Failed to get call ID in mode create: {create}.");
                    return;
                }

                _uiManager.SetJoinCallId(callId);

                _client.SetAudioInputSource(_uiManager.InputAudioSource);
                _client.SetCameraInputSource(_uiManager.InputCameraSource);

                Debug.Log($"Join clicked, create: {create}, callId: {callId}");

                var streamCall
                    = await _client.JoinCallAsync(StreamCallType.Default, callId, create, ring: true, notify: false);

                foreach (var participant in streamCall.Participants)
                {
                    _uiManager.AddParticipant(participant);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}