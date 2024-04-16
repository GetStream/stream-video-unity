#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Shared
{
    public static class TestUtils
    {
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
    }
}
#endif