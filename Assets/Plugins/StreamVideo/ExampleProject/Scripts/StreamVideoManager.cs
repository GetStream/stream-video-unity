using System;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Utils;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField]
        private string _joinCallId = "3TK1d0wL2we0";

        [SerializeField]
        private string _apiKey = "";

        [SerializeField]
        private string _userId = "";

        [SerializeField]
        private string _userToken = "";

        [SerializeField]
        private RawImage _remoteImage;

        private IStreamVideoClient _client;

        private async void OnJoinClicked()
        {
            try
            {
                Debug.Log("Join clicked");

                var create = string.IsNullOrEmpty(_joinCallId);
                var callId = create ? Guid.NewGuid().ToString() : _joinCallId;

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