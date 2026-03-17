namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Guards reconnection attempts based on the current <see cref="CallingState"/>.
    /// Prevents reconnection in states where it would be unsafe or redundant
    /// (e.g. already reconnecting, joining, leaving, or left).
    /// </summary>
    internal class ReconnectGuard
    {
        /// <summary>
        /// Whether a reconnection attempt is currently in progress.
        /// This flag protects against race conditions when multiple peer connections
        /// (Publisher and Subscriber) trigger reconnection simultaneously, before
        /// the <see cref="CallingState"/> has been updated.
        /// </summary>
        public bool IsReconnecting { get; private set; }

        /// <summary>
        /// Attempts to begin a reconnection. Returns true if the guard allows it,
        /// false if the request should be silently ignored.
        /// </summary>
        public bool TryBeginReconnection(CallingState currentState)
        {
            if (IsIgnoredState(currentState))
            {
                return false;
            }

            if (IsReconnecting)
            {
                return false;
            }

            IsReconnecting = true;
            return true;
        }

        /// <summary>
        /// Marks the current reconnection attempt as complete.
        /// Must be called in a finally block to ensure the guard is released.
        /// </summary>
        public void EndReconnection()
        {
            IsReconnecting = false;
        }

        private static bool IsIgnoredState(CallingState state)
            => state == CallingState.Reconnecting
               || state == CallingState.Migrating
               || state == CallingState.Joining
               || state == CallingState.Leaving
               || state == CallingState.Left
               || state == CallingState.ReconnectingFailed;
    }
}
