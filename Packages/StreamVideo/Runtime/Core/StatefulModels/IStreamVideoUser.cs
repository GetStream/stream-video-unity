using StreamVideo.Core.State;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// Represents a Stream Video API user.
    /// Use can get invited to a <see cref="IStreamCall"/> as a <see cref="IStreamCallMember"/> //StreamTodo: fix this summary
    /// And can join the <see cref="IStreamCall"/> as a <see cref="StreamVideoCallParticipant"/>
    /// </summary>
    public interface IStreamVideoUser : IStreamStatefulModel
    {
        string Id { get; }
    }
}