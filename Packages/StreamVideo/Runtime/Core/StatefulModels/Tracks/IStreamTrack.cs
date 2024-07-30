namespace StreamVideo.Core.StatefulModels.Tracks
{
    public interface IStreamTrack
    {
        /// <summary>
        /// Event triggered when the enabled state changes
        /// </summary>
        event StreamTrackStateChangeHandler EnabledChanged;

        /// <summary>
        /// Is this track active.
        /// </summary>
        bool IsEnabled { get; }

        string Id { get; }
    }
}