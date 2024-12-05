using System;
using System.IO;
using UnityEngine;

namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
    /// <summary>
    /// Collects logs when enabled and stores them in a file.
    /// </summary>
    internal class LogsCollector : ILogsCollector
    {
        public LogsCollector()
        {
            _logFilePath = Path.Combine(Application.persistentDataPath, LogsFilePath);
        }

        public void Enable()
        {
            if (_enabled)
            {
                return;
            }

            Application.logMessageReceived += HandleLog;
        }

        public void Disable() => Application.logMessageReceived -= HandleLog;

        public string GetLogs()
        {
            if (!File.Exists(LogsFilePath))
            {
                return string.Empty;
            }

            return File.ReadAllText(_logFilePath);
        }

        public void Dispose()
        {
            if (_enabled)
            {
                Disable();
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            var formattedLog = $"[{DateTime.Now}] [{type}] {logString}\n{stackTrace}";

            File.AppendAllText(_logFilePath, formattedLog + "\n\n");
        }

        private const string LogsFilePath = "unity_session_logs_43534.txt";

        private readonly string _logFilePath;

        private bool _enabled;
    }
#endif
}