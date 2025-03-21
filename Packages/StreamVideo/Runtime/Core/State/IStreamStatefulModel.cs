namespace StreamVideo.Core.State
{
    /// <summary>
    /// Model with its state being automatically updated by the <see cref="IStreamVideoClient"/>
    ///
    /// This means that this object corresponds to an object on the Stream Video server with the same ID
    /// its state will be automatically updated whenever new information is received from the server
    /// </summary>
    public interface IStreamStatefulModel
    {
        string UniqueId { get; }
    }
}