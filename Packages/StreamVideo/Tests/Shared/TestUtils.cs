#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

namespace StreamVideo.Tests.Shared
{
    public static class TestUtils
    {
        public const string StreamVideoPackageName = "io.getstream.video";
        
        public static IEnumerator RunAsIEnumerator(this Task task,
            Action onSuccess = null)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            onSuccess?.Invoke();
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