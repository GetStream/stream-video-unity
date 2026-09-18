using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Platform person-segmenter. Android returns a deferred ML Kit wrapper that reports
    /// support from a classpath check; <c>Segmentation.getClient</c> runs on first enable.
    /// iOS returns a deferred Vision wrapper when iOS 15+ is available; the native client
    /// is created on first enable. Editor and desktop return <see cref="NullPersonSegmenter"/>.
    /// </summary>
    internal static class PersonSegmenterFactory
    {
        public static IPersonSegmenter Create(ILogs logs)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var created = AndroidMlKitPersonSegmenter.TryCreate(logs, out var segmenter)
                ? (IPersonSegmenter)segmenter
                : new NullPersonSegmenter();
#if STREAM_DEBUG_ENABLED
            CameraOrientationDebug.Log(logs, "segmenter.factory",
                "platform=Android created=" + created.GetType().Name + " supported=" + created.IsSupported);
#endif
            return created;
#elif UNITY_IOS && !UNITY_EDITOR
            var created = IosVisionPersonSegmenter.TryCreate(logs, out var segmenter)
                ? (IPersonSegmenter)segmenter
                : new NullPersonSegmenter();
#if STREAM_DEBUG_ENABLED
            CameraOrientationDebug.Log(logs, "segmenter.factory",
                "platform=iOS created=" + created.GetType().Name + " supported=" + created.IsSupported);
#endif
            return created;
#elif UNITY_EDITOR
            var unsupported = new NullPersonSegmenter();
#if STREAM_DEBUG_ENABLED
            CameraOrientationDebug.Log(logs, "segmenter.factory",
                "platform=Editor created=NullPersonSegmenter (Editor has no person segmenter)");
#endif
            return unsupported;
#else
            var unsupported = new NullPersonSegmenter();
#if STREAM_DEBUG_ENABLED
            CameraOrientationDebug.Log(logs, "segmenter.factory",
                "platform=other created=NullPersonSegmenter");
#endif
            return unsupported;
#endif
        }
    }
}
