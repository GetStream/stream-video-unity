#if STREAM_TESTS_ENABLED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework.Interfaces;
using StreamVideo.Core;
using StreamVideo.Core.Configs;
using UnityEngine;

namespace StreamVideo.Tests.Shared
{
    /// <summary>
    /// Maintains global instance of stream chat client to be shared across all tests and disposes them once all of the tests are finished.
    /// This allows a single <see cref="IStreamVideoClient"/> instance to be shared across all test groups.
    /// Every test group should call <see cref="AddLock"/> when starting tests and <see cref="ReleaseLockAsync"/> when done. Once all locks are released the client will be disconnected and disposed.
    /// </summary>
    public class StreamTestClientProvider
    {
        public static StreamTestClientProvider Instance => _instance ??= new StreamTestClientProvider();

        public void AddLock(object owner) => _locks.Add(owner);

        public async Task ReleaseLockAsync(object owner)
        {
            _locks.Remove(owner);
            await TryDisposeInstancesAsync();
        }

        public Task LeaveAllActiveCallsAsync()
        {
            var tasks = new List<Task>();
            foreach (var client in _spawnedClients)
            {
                if (client.ActiveCall != null)
                {
                    tasks.Add(client.ActiveCall.LeaveAsync());
                }
            }

            return Task.WhenAll(tasks);
        }

        public async Task<ITestClient[]> GetConnectedTestClientsAsync(int numberOfClients)
        {
            if (numberOfClients < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(numberOfClients)} must be greater than or equal to 1");
            }
            
            TryStartUpdateTask();

            var clients = GetVideoClientsAsync(numberOfClients);
            ITestClient[] testClients = clients.Select(c => new TestClient(c)).ToArray();
            var connectTasks = testClients.Select(c => c.ConnectAsync());

            await Task.WhenAll(connectTasks);

            return testClients;
        }

        private static StreamTestClientProvider _instance;

        private readonly HashSet<object> _locks = new HashSet<object>();
        private readonly List<IStreamVideoClient> _spawnedClients = new List<IStreamVideoClient>();

        private bool _runFinished;
        private Task _updateTask;
        private CancellationTokenSource _updateTaskCts;

        private StreamTestClientProvider()
        {
            UnityTestRunnerCallbacks.RunFinishedCallback += OnRunFinishedCallback;
        }
        
        private IEnumerable<IStreamVideoClient> GetVideoClientsAsync(int numberOfClients)
        {
            var returnedClients = 0;

            foreach (var client in _spawnedClients)
            {
                if (returnedClients >= numberOfClients)
                {
                    yield break;
                }

                yield return client;
                returnedClients++;
            }

            while (returnedClients < numberOfClients)
            {
                yield return CreateStateClient();
                returnedClients++;
            }
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

        private IStreamVideoClient CreateStateClient()
        {
            var client = StreamVideoClient.CreateDefaultClient(new StreamClientConfig
            {
                LogLevel = StreamLogLevel.Debug
            });

            _spawnedClients.Add(client);
            return client;
        }

        private async Task DisposeStateClientsAsync()
        {
            TryStopUpdateTask();

            var tasks = new List<Task>();
            tasks.AddRange(_spawnedClients.Select(async c =>
            {
                await c.DisconnectAsync();
                c.Dispose();
            }));

            await Task.WhenAll(tasks);
            _spawnedClients.Clear();
        }

        private void TryStartUpdateTask()
        {
            if (_updateTask != null)
            {
                return;
            }

            _updateTaskCts = new CancellationTokenSource();
            _updateTask = UpdateTaskAsync();

            _updateTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError(t.Exception);
                }
            });
        }

        private void TryStopUpdateTask()
        {
            Debug.LogWarning("TryStopUpdateTask");
            _updateTaskCts?.Cancel();
        }

        private async Task UpdateTaskAsync()
        {
            Debug.LogWarning("UpdateTaskAsync STARTED");
            while (!_updateTaskCts.Token.IsCancellationRequested)
            {
                try
                {
                    _updateTaskCts.Token.ThrowIfCancellationRequested();
                }
                catch (Exception)
                {
                    Debug.LogWarning("UpdateTaskAsync STOPPED");
                    throw;
                }

                foreach (var client in _spawnedClients)
                {
                    client.Update();
                }

                await Task.Delay(1);
            }
        }
    }
}
#endif