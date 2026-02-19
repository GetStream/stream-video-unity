using System;

namespace StreamVideo.Core.LowLevelClient
{
    internal class SessionID
    {
        public bool IsEmpty => string.IsNullOrEmpty(_sessionID);
        
        /// <summary>
        /// Version counter that increments each time the session is regenerated.
        /// Used to invalidate stale operations (e.g., ICE restart requests that were queued
        /// before a rejoin completed with a new session ID).
        /// </summary>
        public int Version { get; private set; }

        public void Regenerate()
        {
            _sessionID = Guid.NewGuid().ToString();
            Version++;
        }

        public void Clear() => _sessionID = string.Empty;

        public override string ToString() => _sessionID ?? string.Empty;

        private string _sessionID;
    }
}
