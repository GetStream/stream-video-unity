using System.Collections.Generic;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core
{
    public readonly struct QueryCallsResult
    {
        public readonly List<IStreamCall> Calls;
        public readonly string Next;
        public readonly string Prev;

        internal QueryCallsResult(List<IStreamCall> calls, string next, string prev)
        {
            Calls = calls;
            Next = next;
            Prev = prev;
        }
    }
}