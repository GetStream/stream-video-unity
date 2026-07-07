#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using UnityEngine.TestTools;
using UnityEditor.PackageManager;

namespace StreamVideo.Tests.Shared
{
    public static class TestUtils
    {
        public const string StreamVideoPackageName = "io.getstream.video";

        /// <summary>
        /// Polls <paramref name="condition"/> until it returns true or the timeout elapses,
        /// then asserts it is true with <paramref name="timeoutMessage"/>.
        /// </summary>
        public static async Task WaitUntilAsync(Func<bool> condition, string timeoutMessage, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }

            Assert.That(condition(), Is.True, timeoutMessage);
        }
        
        public static IEnumerator RunAsIEnumerator(this Task task,
            Action onSuccess = null, bool ignoreFailingMessages = false)
        {
            if (ignoreFailingMessages)
            {
                LogAssert.ignoreFailingMessages = true;
            }

            while (!task.IsCompleted)
            {
                //StreamTODO: implement timeout
                yield return null;
            }

            if (ignoreFailingMessages)
            {
                LogAssert.ignoreFailingMessages = false;
            }

            if (task.IsFaulted)
            {
                throw UnwrapAggregateException(task.Exception);
            }

            onSuccess?.Invoke();
        }
        
        //StreamTodo: put this in VideoDeviceManager?
        public static async Task<CameraDeviceInfo> TryGetFirstWorkingCameraDeviceAsync(IStreamVideoClient client)
        {
            var cameraManager = client.VideoDeviceManager;
            foreach (var cameraDevice in cameraManager.EnumerateDevices())
            {
                var isWorking = await cameraManager.TestDeviceAsync(cameraDevice, 0.5f);
                if (isWorking)
                {
                    return cameraDevice;
                }
            }

            return cameraManager.EnumerateDevices().First();
        }

        public static async Task<PackageInfo> GetStreamVideoPackageInfo()
        {
            var listPackagesRequest = Client.List();

            while (!listPackagesRequest.IsCompleted)
            {
                await Task.Delay(1);
            }

            // Get unity package
            var packages = listPackagesRequest.Result;
            return packages.First(p => p.name == StreamVideoPackageName);
        }
        
        private static Exception UnwrapAggregateException(Exception exception)
        {
            if (exception is AggregateException aggregateException &&
                aggregateException.InnerExceptions.Count == 1)
            {
                return aggregateException.InnerExceptions[0];
            }

            return exception;
        }
    }
}
#endif