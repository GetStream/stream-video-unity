using System.Collections.Generic;
using StreamVideo.Core.Models;

namespace StreamVideo.Core
{
    public readonly struct QueryMembersResult
    {
        public readonly List<CallMember> Members;
        public readonly string Next;
        public readonly string Prev;

        internal QueryMembersResult(List<CallMember> members, string next, string prev)
        {
            Members = members;
            Next = next;
            Prev = prev;
        }
    }
}