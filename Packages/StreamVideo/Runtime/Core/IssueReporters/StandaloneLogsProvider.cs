using System;
using System.IO;
using UnityEngine;

namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
    internal class StandaloneLogsProvider : ILogsProvider
    {
        public string GetLogs()
        {
            var logPath = GetLogPath();

            try
            {
                if (!File.Exists(logPath))
                {
                    return $"Log file not found at: `{logPath}` for platform: {Application.platform}";
                }

                // Use FileShare.ReadWrite to allow reading while Unity is writing to it
                using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to read log file: {ex.Message}");
                return string.Empty;
            }
        }

        private static string GetLogPath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Unity", "Editor", "Editor.log");

                case RuntimePlatform.OSXEditor:
                    var osxEditorHome = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    return Path.Combine(osxEditorHome, "Library", "Logs", "Unity", "Editor.log");

                case RuntimePlatform.LinuxEditor:
                    var linuxEditorHome = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    return Path.Combine(linuxEditorHome, ".config", "unity3d", "Editor.log");

                case RuntimePlatform.WindowsPlayer:
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "LocalLow",
                        Application.companyName,
                        Application.productName,
                        "Player.log");

                case RuntimePlatform.OSXPlayer:
                    var osxPlayerHome = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    return Path.Combine(osxPlayerHome, "Library", "Logs",
                        Application.companyName,
                        Application.productName,
                        "Player.log");

                case RuntimePlatform.LinuxPlayer:
                    var linuxPlayerHome = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    return Path.Combine(linuxPlayerHome, ".config", "unity3d",
                        Application.companyName,
                        Application.productName,
                        "Player.log");

                default:
                    return string.Empty;
            }
        }
    }
#endif
}