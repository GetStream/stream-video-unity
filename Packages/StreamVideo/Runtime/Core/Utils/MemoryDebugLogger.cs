#if STREAM_DEBUG_ENABLED
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using StreamVideo.Libs.Logs;
using UnityEngine;
using UnityEngine.Profiling;

namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// Grep logs with "[Memory]" during call join/leave and periodic stats.
    /// Requires STREAM_DEBUG_ENABLED (Stream Video SDK debug mode in Project Settings).
    /// </summary>
    internal readonly struct ProcessMemorySnapshot
    {
        public ProcessMemorySnapshot(long managedBytes, long unityAllocatedBytes, long unityReservedBytes,
            long processFootprintBytes, ProcessFootprintKind footprintKind)
        {
            ManagedBytes = managedBytes;
            UnityAllocatedBytes = unityAllocatedBytes;
            UnityReservedBytes = unityReservedBytes;
            ProcessFootprintBytes = processFootprintBytes;
            FootprintKind = footprintKind;
        }

        public long ManagedBytes { get; }
        public long UnityAllocatedBytes { get; }
        public long UnityReservedBytes { get; }
        public long ProcessFootprintBytes { get; }
        public ProcessFootprintKind FootprintKind { get; }
    }

    internal enum ProcessFootprintKind
    {
        Unavailable = 0,
        IosPhysicalFootprint = 1,
        AndroidNativeHeap = 2,
        WorkingSet = 3,
    }

    internal static class ProcessMemoryMetrics
    {
        public static ProcessMemorySnapshot Capture()
        {
            var managed = GC.GetTotalMemory(false);
            var unityAllocated = Profiler.GetTotalAllocatedMemoryLong();
            var unityReserved = Profiler.GetTotalReservedMemoryLong();
            var (footprint, kind) = TryGetProcessFootprint();

            return new ProcessMemorySnapshot(managed, unityAllocated, unityReserved, footprint, kind);
        }

        private static (long bytes, ProcessFootprintKind kind) TryGetProcessFootprint()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var iosFootprint = TryGetIosPhysicalFootprint();
            if (iosFootprint >= 0)
            {
                return (iosFootprint, ProcessFootprintKind.IosPhysicalFootprint);
            }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            var androidNative = TryGetAndroidNativeHeapBytes();
            if (androidNative >= 0)
            {
                return (androidNative, ProcessFootprintKind.AndroidNativeHeap);
            }
#endif
            try
            {
                return (Process.GetCurrentProcess().WorkingSet64, ProcessFootprintKind.WorkingSet);
            }
            catch (Exception)
            {
                return (-1, ProcessFootprintKind.Unavailable);
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        private const int TASK_VM_INFO = 22;

        [DllImport("/usr/lib/libSystem.B.dylib")]
        private static extern int mach_task_self();

        [DllImport("/usr/lib/libSystem.B.dylib")]
        private static extern int task_info(int targetTask, int flavor, ref TaskVmInfoData info, ref int count);

        [StructLayout(LayoutKind.Sequential)]
        private struct TaskVmInfoData
        {
            public ulong virtual_size;
            public ulong region_count;
            public ulong page_size;
            public ulong resident_size;
            public ulong resident_size_peak;
            public ulong device;
            public ulong device_peak;
            public ulong internal_size;
            public ulong internal_peak;
            public ulong external_size;
            public ulong external_peak;
            public ulong reusable_size;
            public ulong reusable_peak;
            public ulong phys_footprint;
        }

        private static long TryGetIosPhysicalFootprint()
        {
            var info = new TaskVmInfoData();
            var count = Marshal.SizeOf<TaskVmInfoData>() / sizeof(int);
            var result = task_info(mach_task_self(), TASK_VM_INFO, ref info, ref count);
            return result == 0 ? (long)info.phys_footprint : -1;
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static long TryGetAndroidNativeHeapBytes()
        {
            try
            {
                using var debug = new AndroidJavaClass("android.os.Debug");
                return debug.CallStatic<long>("getNativeHeapAllocatedSize");
            }
            catch (Exception)
            {
                return -1;
            }
        }
#endif
    }

    internal static class MemoryDebugLogger
    {
        private const string Tag = "[Memory]";

        public static void Log(ILogs logs, string milestone, string context = null)
        {
            if (logs == null)
            {
                throw new ArgumentNullException(nameof(logs));
            }

            var snapshot = ProcessMemoryMetrics.Capture();
            var contextSuffix = string.IsNullOrEmpty(context) ? string.Empty : $" | {context}";
            logs.Warning(
                $"{Tag} {milestone}{contextSuffix} | managed={FormatBytes(snapshot.ManagedBytes)} " +
                $"unityAllocated={FormatBytes(snapshot.UnityAllocatedBytes)} " +
                $"unityReserved={FormatBytes(snapshot.UnityReservedBytes)} " +
                $"{FormatFootprint(snapshot)}");
        }

        private static string FormatFootprint(ProcessMemorySnapshot snapshot)
        {
            if (snapshot.ProcessFootprintBytes < 0)
            {
                return "processFootprint=unavailable";
            }

            var label = snapshot.FootprintKind switch
            {
                ProcessFootprintKind.IosPhysicalFootprint => "iosPhysFootprint",
                ProcessFootprintKind.AndroidNativeHeap => "androidNativeHeap",
                ProcessFootprintKind.WorkingSet => "workingSet",
                _ => "processFootprint",
            };

            return $"{label}={FormatBytes(snapshot.ProcessFootprintBytes)}";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 0)
            {
                return "n/a";
            }

            const double mb = 1024d * 1024d;
            return $"{bytes / mb:F1}MB";
        }
    }
}
#endif
