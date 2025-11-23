using System.Collections.Generic;

namespace StreamVideo.Core.Utils
{
    internal class DebugLogBuffer
    {
        private const int MaxSize = 10;
        private readonly string[] _buffer = new string[MaxSize];
        private int _index;
        private int _count;

        public void Add(string msg)
        {
            _buffer[_index] = msg;
            _index = (_index + 1) % MaxSize;
            if (_count < MaxSize) _count++;
        }

        public IEnumerable<string> GetLogs()
        {
            for (var i = 0; i < _count; i++)
            {
                var idx = (_index - _count + i + MaxSize) % MaxSize;
                yield return _buffer[idx];
            }
        }
    }
}