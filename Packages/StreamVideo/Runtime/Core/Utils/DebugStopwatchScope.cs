using System;
using System.Diagnostics;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// [ONLY IF STREAM_DEBUG_ENABLED] Measure scope execution. 
    /// </summary>
    internal class DebugStopwatchScope : IDisposable
    {
        public enum Units
        {
            MilliSeconds,
            Seconds,
            Minutes
        }

        public DebugStopwatchScope(ILogs logs, string message, Units units = Units.MilliSeconds)
        {
#if STREAM_DEBUG_ENABLED
            _message = !message.IsNullOrEmpty() ? message : throw new ArgumentException("");
            _units = units;
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _sw = new Stopwatch();
            _sw.Start();
#endif
        }

        private readonly ILogs _logs;
        private readonly Stopwatch _sw;
        private readonly Units _units;
        private readonly string _message;

        public void Dispose()
        {
#if STREAM_DEBUG_ENABLED
            _sw.Stop();
            _logs.Warning($"`{_message}` executed in: {GetTimeLog()}");
#endif
        }

        private string GetTimeLog()
        {
            switch (_units)
            {
                case Units.MilliSeconds: return $"{_sw.Elapsed.TotalMilliseconds} ms";
                case Units.Seconds: return $"{_sw.Elapsed.TotalSeconds} s";
                case Units.Minutes: return $"{_sw.Elapsed.TotalMinutes} min";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}