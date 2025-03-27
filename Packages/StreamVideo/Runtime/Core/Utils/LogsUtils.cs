using System;
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
        
        public static void ExceptionIfDebug(this ILogs logs, Exception exception)
        {
#if STREAM_DEBUG_ENABLED
            logs.Exception(exception);
#endif
        }
    }
}