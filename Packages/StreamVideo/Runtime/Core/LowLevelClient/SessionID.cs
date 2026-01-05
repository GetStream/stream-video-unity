using System;

namespace StreamVideo.Core.LowLevelClient
{
    internal class SessionID
    {
        public bool IsEmpty => string.IsNullOrEmpty(_sessionID);

        public void Regenerate() => _sessionID = Guid.NewGuid().ToString();

        public void Clear() => _sessionID = string.Empty;

        public static implicit operator string(SessionID s) => s._sessionID;

        private string _sessionID;
    }
}