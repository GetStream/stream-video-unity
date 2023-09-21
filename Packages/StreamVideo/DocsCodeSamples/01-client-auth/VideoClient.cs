using System;
using StreamVideo.Core;
using StreamVideo.Libs.Auth;
using UnityEngine;

namespace StreamVideo.DocsCodeSamples._01_client_auth
{
    public class VideoClient : MonoBehaviour
    {
        async void Start()
        {
            _client = StreamVideoClient.CreateDefaultClient();

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