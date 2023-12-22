#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.StatefulModels;
using UnityEngine;

namespace StreamVideo.Tests.Shared
{
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
            await StreamTestClientProvider.Instance.ReleaseLockAsync(this);
        }

        [TearDown]
        public async void TearDown()
        {
            Debug.LogWarning("Every time tear down");

            if (Client.ActiveCall != null)
            {
                Debug.LogWarning("Call was active -> leave");
                await Client.ActiveCall.LeaveAsync();
            }
        }

        public async Task<IStreamCall> JoinRandomCallAsync()
        {
            var callId = Guid.NewGuid().ToString();
            return await Client.JoinCallAsync(StreamCallType.Default, callId, create: true, ring: false,
                notify: false);
        }

        protected static IStreamVideoClient Client => StreamTestClientProvider.Instance.StateClient;
        
        protected static IEnumerator ConnectAndExecute(Func<Task> test)
        {
            yield return ConnectAndExecuteAsync(test).RunAsIEnumerator(statefulClient: Client);
        }
        
        private static async Task ConnectAndExecuteAsync(Func<Task> test)
        {
            await StreamTestClientProvider.Instance.ConnectStateClientAsync();
            const int maxAttempts = 7;
            var currentAttempt = 0;
            var completed = false;
            var exceptions = new List<Exception>();
            while (maxAttempts > currentAttempt)
            {
                currentAttempt++;
                try
                {
                    await test();
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