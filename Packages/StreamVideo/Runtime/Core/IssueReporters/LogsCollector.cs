using System;
using System.IO;
using System.Text;
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

            ClearLogFile();
        }

        public void Enable()
        {
            if (_enabled)
            {
                return;
            }

            Application.logMessageReceived += HandleLog;
            _enabled = true;
        }

        public void Disable() => Application.logMessageReceived -= HandleLog;

        public string GetLogs()
        {
            if (!File.Exists(_logFilePath))
            {
                return string.Empty;
            }

            try
            {
                using (var fileStream = File.Open(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return string.Empty;
            }
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

            using (var fileStream = File.Open(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var writeStream = new StreamWriter(fileStream))
                {
                    writeStream.WriteLine(formattedLog + "\n");
                }
            }
        }

        private const string LogsFilePath = "unity_session_logs_43534.txt";

        private readonly string _logFilePath;

        private bool _enabled;

        private void ClearLogFile()
        {
            if (!File.Exists(_logFilePath))
            {
                return;
            }

            using (File.Open(_logFilePath, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite))
            {
            }
        }
    }
#endif
}