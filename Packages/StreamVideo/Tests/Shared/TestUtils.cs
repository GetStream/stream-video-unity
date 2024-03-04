#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Threading.Tasks;

namespace StreamVideo.Tests.Shared
{
    public static class TestUtils
    {
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
    }
}
#endif