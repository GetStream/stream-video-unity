#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using UnityEngine.TestTools;
using UnityEditor.PackageManager;

namespace StreamVideo.Tests.Shared
{
    public static class TestUtils
    {
        public const string StreamVideoPackageName = "io.getstream.video";
        
        public static IEnumerator RunAsIEnumerator(this Task task,
            Action onSuccess = null, bool ignoreFailingMessages = false)
        {
            if (ignoreFailingMessages)
            {
                LogAssert.ignoreFailingMessages = true;
            }

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (ignoreFailingMessages)
            {
                LogAssert.ignoreFailingMessages = false;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
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
    }
}
#endif