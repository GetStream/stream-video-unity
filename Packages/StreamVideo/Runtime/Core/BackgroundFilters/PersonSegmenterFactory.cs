using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.BackgroundFilters
{
    internal static class PersonSegmenterFactory
    {
        public static IPersonSegmenter Create(ILogs logs)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidMlKitPersonSegmenter.TryCreate(logs, out var segmenter)
                ? (IPersonSegmenter)segmenter
                : new NullPersonSegmenter();
#elif UNITY_EDITOR
            return new EditorStubPersonSegmenter();
#else
            return new NullPersonSegmenter();
#endif
        }
    }
}
