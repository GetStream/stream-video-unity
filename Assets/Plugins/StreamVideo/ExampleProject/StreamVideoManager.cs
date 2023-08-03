using System;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Utils;
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
            var apiKey = "hd8szvscpxvd";
            var userId = "daniel_sierpinski";
            var userToken
                = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiZGFuaWVsX3NpZXJwaW5za2kiLCJpc3MiOiJwcm9udG8iLCJzdWIiOiJ1c2VyL2RhbmllbF9zaWVycGluc2tpIiwiaWF0IjoxNjkwODkzMjQyLCJleHAiOjE2OTA5MDc2NDd9.wW2te_0MkxmzdOYd4a6KyOUbxMrdgeP7K79msMINonQ";

            var credentials = new AuthCredentials(apiKey, userId, userToken);

            _client = StreamVideoClient.CreateDefaultClient(new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug
            });

            _client.ConnectUserAsync(credentials).LogIfFailed();
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

        private IStreamVideoClient _client;
        
        private async void OnJoinClicked()
        {
            try
            {
                Debug.Log("Join clicked");

                if (!string.IsNullOrEmpty(_joinCallId))
                {
                    await _client.JoinCallAsync(StreamCallType.Default, _joinCallId, create: false, ring: true,
                        notify: false);
                }
                else
                {
                    await _client.JoinCallAsync(StreamCallType.Development, Guid.NewGuid().ToString(), create: true, ring: true,
                        notify: false);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}