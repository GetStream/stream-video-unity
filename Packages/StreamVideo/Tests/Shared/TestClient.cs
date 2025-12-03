#if STREAM_TESTS_ENABLED
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Serialization;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace StreamVideo.Tests.Shared
{
    public class TestClient : ITestClient
    {
        public IStreamVideoClient Client { get; }

        public TestClient(IStreamVideoClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<IStreamCall> JoinRandomCallAsync()
        {
            if (Client.ActiveCall != null)
            {
                await Client.ActiveCall.LeaveAsync();
            }
            
            var callId = Guid.NewGuid().ToString();
            return await Client.JoinCallAsync(StreamCallType.Default, callId, create: true, ring: false,
                notify: false);
        }

        public async Task CleanupAfterSingleTestSessionAsync()
        {
            if (Client.ActiveCall != null)
            {
                await Client.ActiveCall.LeaveAsync();
            }
        }

        public async Task ConnectAsync()
        {
            if (Client.IsConnected)
            {
                return;
            }

            const int timeoutMs = 10 * 1000;
            var timer = new Stopwatch();
            timer.Start();

            var getCredentialsTask = GetStreamDemoCredentialsAsync();
            while (!getCredentialsTask.IsCompleted)
            {
                await Task.Delay(1);

                if (timer.ElapsedMilliseconds > timeoutMs)
                {
                    throw new TimeoutException(
                        $"Reached timeout when trying to get credentials. Ms passed: {timer.ElapsedMilliseconds}");
                }
            }

            var demoCredentials = getCredentialsTask.Result;
            var credentials
                = new AuthCredentials(demoCredentials.APIKey, demoCredentials.UserId, demoCredentials.Token);

            var connectTask = Client.ConnectUserAsync(credentials);

#if STREAM_DEBUG_ENABLED
            Debug.Log($"Wait for {nameof(Client)} to connect user with ID: {credentials.UserId}");
#endif

            while (!connectTask.IsCompleted)
            {
                await Task.Delay(1);

                if (timer.ElapsedMilliseconds > timeoutMs)
                {
                    throw new TimeoutException($"Reached timeout when trying to connect user: {credentials.UserId}");
                }
            }

            timer.Stop();

            Debug.Log($"Client connected in {timer.Elapsed.TotalSeconds:F2} seconds");
        }

        private class DemoCredentialsApiResponse
        {
            public string UserId;
            public string Token;
            public string APIKey;
        }

        private class DemoCredentialsApiError
        {
            public string Error;
        }

        private static async Task<DemoCredentialsApiResponse> GetStreamDemoCredentialsAsync()
        {
            Debug.Log("Get demo credentials " + Time.time);
            var serializer = new NewtonsoftJsonSerializer();
            var httpClient = new HttpClient();
            var uriBuilder = new UriBuilder
            {
                Host = "pronto.getstream.io",
                Path = "/api/auth/create-token",
                Query = $"user_id=DemoUser",
                Scheme = "https",
            };

            var uri = uriBuilder.Uri;
            var response = await httpClient.GetAsync(uri);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var apiError = serializer.Deserialize<DemoCredentialsApiError>(result);
                throw new Exception(
                    $"Failed to get demo credentials. Error status code: `{response.StatusCode}`, Error message: `{apiError.Error}`");
            }

            Debug.Log("Demo credentials received: " + Time.time);

            return serializer.Deserialize<DemoCredentialsApiResponse>(result);
        }
    }
}
#endif