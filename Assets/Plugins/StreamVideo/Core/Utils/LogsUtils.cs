using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.Utils
{
    internal static class LogsUtils
    {
        public static void InfoIfDebug(this ILogs logs, string message)
        {
#if STREAM_DEBUG_ENABLED
            logs.Info(message);
#endif
        }

        public static void WarningIfDebug(this ILogs logs, string message)
        {
#if STREAM_DEBUG_ENABLED
            logs.Warning(message);
#endif
        }

        public static void ErrorIfDebug(this ILogs logs, string message)
        {
#if STREAM_DEBUG_ENABLED
            logs.Error(message);
#endif
        }
    }
}