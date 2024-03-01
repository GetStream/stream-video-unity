#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core.Exceptions;
using StreamVideo.Tests.Shared.DisposableAssets;
using Debug = UnityEngine.Debug;

namespace StreamVideo.Tests.Shared
{
    public delegate Task SingleClientTestHandler(ITestClient client);
    public delegate Task TwoClientsTestHandler(ITestClient client1, ITestClient client2);
    
    public class TestsBase
    {
        [OneTimeSetUp]
        public void OneTimeUp()
        {
            StreamTestClientProvider.Instance.AddLock(this);
        }

        [OneTimeTearDown]
        public async void OneTimeTearDown()
        {
            Debug.LogWarning("[One Time] TearDown");
            await StreamTestClientProvider.Instance.ReleaseLockAsync(this);
        }

        [TearDown]
        public async void TearDown()
        {
            Debug.LogWarning("[Per Test] TearDown");

            await StreamTestClientProvider.Instance.LeaveAllActiveCallsAsync();
            DisposableAssetsProvider.DisposeInstances();
        }

        protected DisposableAssetsProvider DisposableAssetsProvider { get; } = new DisposableAssetsProvider();

        protected virtual void OnTearDown()
        {
            
        }

        protected static async Task<(bool, TimeSpan)> WaitForConditionAsync(Func<bool> condition, int timeoutMs = 2000)
        {
            if (condition())
            {
                return (true, TimeSpan.Zero);
            }
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(1);

                if (condition())
                {
                    return (true, stopwatch.Elapsed);
                }
            }
            
            return (false, stopwatch.Elapsed);
        }
        
        protected static IEnumerator ConnectAndExecute(Func<Task> test)
        {
            yield return ConnectAndExecuteAsync(_ => test()).RunAsIEnumerator();
        }
        
        protected static IEnumerator ConnectAndExecute(SingleClientTestHandler test)
        {
            yield return ConnectAndExecuteAsync(clients => test(clients[0]), clientsToSpawn: 1).RunAsIEnumerator();
        }
        
        protected static IEnumerator ConnectAndExecute(TwoClientsTestHandler test)
        {
            yield return ConnectAndExecuteAsync(clients => test(clients[0], clients[1]), clientsToSpawn: 2).RunAsIEnumerator();
        }
        
        private static async Task ConnectAndExecuteAsync(Func<ITestClient[], Task> test, int clientsToSpawn = 1)
        {
            var clients = await StreamTestClientProvider.Instance.GetConnectedTestClientsAsync(clientsToSpawn);
            const int maxAttempts = 7;
            var currentAttempt = 0;
            var completed = false;
            var exceptions = new List<Exception>();
            while (maxAttempts > currentAttempt)
            {
                currentAttempt++;
                try
                {
                    await test(clients);
                    completed = true;
                    break;
                }
                catch (StreamApiException e)
                {
                    exceptions.Add(e);
                    if (e.IsRateLimitExceededError())
                    {
                        var seconds = (int)Math.Max(1, Math.Min(60, Math.Pow(2, currentAttempt)));
                        await Task.Delay(1000 * seconds);
                        continue;
                    }

                    throw;
                }
            }

            if (!completed)
            {
                throw new AggregateException($"Failed all attempts. Last Exception: {exceptions.Last().Message} ", exceptions);
            }
        }
    }
}
#endif