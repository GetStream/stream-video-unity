using StreamVideo.Core.State;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// A <see cref="IStreamVideoUser"/> that is actively connected to a <see cref="IStreamCall"/>
    /// </summary>
    public interface IStreamVideoCallParticipant : IStreamStatefulModel
    {
    }
}