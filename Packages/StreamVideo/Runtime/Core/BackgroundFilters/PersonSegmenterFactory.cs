using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.BackgroundFilters
{
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
                "platform=other created=NullPersonSegmenter (iOS Vision is not implemented yet)");
#endif
            return unsupported;
#endif
        }
    }
}
