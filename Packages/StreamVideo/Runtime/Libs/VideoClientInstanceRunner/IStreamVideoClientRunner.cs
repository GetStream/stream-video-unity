namespace StreamVideo.Libs.VideoClientInstanceRunner
{
    /// <summary>
    /// Runner is responsible for calling callbacks on the <see cref="IStreamVideoClientEventsListener"/>
    /// </summary>
    public interface IStreamVideoClientRunner
    {
        /// <summary>
        /// Pass environment callbacks to the <see cref="IStreamVideoClientEventsListener"/> and react to its events
        /// </summary>
        void RunClientInstance(IStreamVideoClientEventsListener streamVideoInstance);
    }
}