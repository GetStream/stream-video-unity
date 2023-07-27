using System;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Libs.Auth;
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
            var apiKey = "r94gjesrb59f";
            var userId = "yoda-admin";
            var userToken
                = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoieW9kYS1hZG1pbiJ9.2wVvXVatKCl2J8QACpDdUvGW48e7qYL9nlYgOXF-2L4";

            var credentials = new AuthCredentials(apiKey, userId, userToken);

            _client = StreamVideoLowLevelClient.CreateDefaultClient(credentials, new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug
            });

            _client.ConnectUser(credentials);
        }

        protected void Update()
        {
            _client?.Update(Time.deltaTime);
        }

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

        private IStreamVideoLowLevelClient _client;
        
        private async void OnJoinClicked()
        {
            try
            {
                Debug.Log("Join clicked");

                await _client.JoinCallAsync(StreamCallType.Development, Guid.NewGuid().ToString(), create: true, ring: true,
                    notify: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}