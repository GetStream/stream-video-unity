#if STREAM_TESTS_ENABLED
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs;
using StreamVideo.Libs.Auth;
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

            var factory = new StreamDependenciesFactory();
            var provider = factory.CreateDemoCredentialsProvider();

            var getCredentialsTask = provider.GetDemoCredentialsAsync("DemoUser", StreamEnvironment.Demo);
            while (!getCredentialsTask.IsCompleted)
            {
                await Task.Delay(1);

                if (timer.ElapsedMilliseconds > timeoutMs)
                {
                    throw new TimeoutException(
                        $"Reached timeout when trying to get credentials. Ms passed: {timer.ElapsedMilliseconds}");
                }
            }

            var credentials = getCredentialsTask.Result;

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

    }
}
#endif