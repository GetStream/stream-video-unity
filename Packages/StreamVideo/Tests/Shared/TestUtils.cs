﻿#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Threading.Tasks;
using StreamVideo.Core;

namespace StreamVideo.Tests.Shared
{
    public static class TestUtils
    {
        public static IEnumerator RunAsIEnumerator(this Task task,
            Action onSuccess = null, IStreamVideoClient statefulClient = null)
        {
            while (!task.IsCompleted)
            {
                statefulClient?.Update();
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