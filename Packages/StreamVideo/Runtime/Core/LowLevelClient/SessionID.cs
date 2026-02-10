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

        public static implicit operator string(SessionID s) => s?._sessionID;

        public override string ToString() => string.IsNullOrEmpty(_sessionID) ? "[Empty]" : _sessionID;

        private string _sessionID;
    }
}