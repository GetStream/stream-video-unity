namespace StreamVideo.Core.LowLevelClient
{
    public enum CallingState
    {
        /// <summary>
        /// The call is in an unknown state.
        /// </summary>
        Unknown,

        /// <summary>
        /// The call is in an idle state.
        /// </summary>
        Idle,

        /// <summary>
        /// The call is in the process of ringing.
        /// (User hasn't accepted nor rejected the call yet.)
        /// </summary>
        Ringing,

        /// <summary>
        /// The call is in the process of joining.
        /// </summary>
        Joining,

        /// <summary>
        /// The call is currently active.
        /// </summary>
        Joined,

        /// <summary>
        /// The call has been left.
        /// </summary>
        Left,

        /// <summary>
        /// The call is in the process of reconnecting.
        /// </summary>
        Reconnecting,

        /// <summary>
        /// The call is in the process of migrating from one node to another.
        /// </summary>
        Migrating,

        /// <summary>
        /// The call has failed to reconnect.
        /// </summary>
        ReconnectingFailed,

        /// <summary>
        /// The call is in offline mode.
        /// </summary>
        Offline
    }
}