using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework.Interfaces;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Serialization;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace StreamVideo.Tests
{
    /// <summary>
    /// Maintains global instance of stream chat client to be shared across all tests and disposes them once all of the tests are finished
    /// </summary>
    internal class StreamTestClientProvider
    {
        public static StreamTestClientProvider Instance => _instance ??= new StreamTestClientProvider();

        public void AddLock(object owner) => _locks.Add(owner);

        public async Task RemoveLockAsync(object owner)
        {
            _locks.Remove(owner);
            await TryDisposeInstancesAsync();
        }

        public IStreamVideoClient StateClient => _client ??= CreateStateClient();

        public Task ConnectStateClientAsync() => ConnectStateClientAsync(StateClient);
        
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

        private static StreamTestClientProvider _instance;

        private readonly HashSet<object> _locks = new HashSet<object>();

        private IStreamVideoClient _client;
        private bool _runFinished;

        private StreamTestClientProvider()
        {
            UnityTestRunnerCallbacks.RunFinishedCallback += OnRunFinishedCallback;
        }

        private static async Task ConnectStateClientAsync(IStreamVideoClient client)
        {
            if (client.IsConnected)
            {
                return;
            }

            const int timeout = 5000;
            var timer = new Stopwatch();
            timer.Start();

            var getCredentialsTask = GetStreamDemoCredentialsAsync();
            while (!getCredentialsTask.IsCompleted)
            {
                client.Update();
                await Task.Delay(1);
                
                if (timer.ElapsedMilliseconds > timeout)
                {
                    throw new TimeoutException($"Reached timeout when trying to get credentials. Ms passed: {timer.ElapsedMilliseconds}");
                }
            }

            var demoCredentials = getCredentialsTask.Result;
            var credentials
                = new AuthCredentials(demoCredentials.APIKey, demoCredentials.UserId, demoCredentials.Token);
            
            var connectTask = client.ConnectUserAsync(credentials);
            while (!connectTask.IsCompleted)
            {
#if STREAM_DEBUG_ENABLED
                Debug.Log($"Wait for {nameof(client)} to connect user with ID: {credentials.UserId}");
#endif

                client.Update();
                await Task.Delay(1);

                if (timer.ElapsedMilliseconds > timeout)
                {
                    throw new TimeoutException($"Reached timeout when trying to connect user: {credentials.UserId}");
                }
            }
            
            timer.Stop();

            Debug.Log($"State client connected. after {timer.Elapsed.TotalSeconds}");
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

        private void OnRunFinishedCallback(ITestResult obj)
        {
            _runFinished = true;
            TryDisposeInstancesAsync();
        }

        private Task TryDisposeInstancesAsync()
        {
            if (!_runFinished || _locks.Any())
            {
                return Task.CompletedTask;
            }

            Debug.Log("------------  Tests finished - dispose client instances");

            return DisposeStateClientsAsync();
        }

        private static IStreamVideoClient CreateStateClient()
            => StreamVideoClient.CreateDefaultClient(new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug
            });

        private async Task DisposeStateClientsAsync()
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
                _client.Dispose();
                _client = null;
            }
        }
    }
}
