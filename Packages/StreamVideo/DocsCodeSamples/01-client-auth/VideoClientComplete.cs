using System;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Libs.Auth;
using UnityEngine;

namespace StreamVideo.DocsCodeSamples._01_client_auth
{
    public class VideoClientComplete : MonoBehaviour
    {
        async void Start()
        {
            var config = new StreamClientConfig
            {
                // Enabling Debug level logging can be helpful during development
                LogLevel = StreamLogLevel.Debug,
                Audio =
                {
                    // Increase audio quality in exchange for higher bandwidth 
                    EnableRed = false,

                    // DTX encodes silence at lower bitrate. This can save bandwidth
                    EnableDtx = false
                }
            };
            _client = StreamVideoClient.CreateDefaultClient(config);

            try
            {
                var authCredentials = new AuthCredentials("api-key", "user-id", "user-token");
                await _client.ConnectUserAsync(authCredentials);

                // After we awaited the ConnectUserAsync the client is connected
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        private IStreamVideoClient _client;
    }
}