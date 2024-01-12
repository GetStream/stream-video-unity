using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.State
{
    public interface IHasCustomData
    {
        /// <summary>
        /// Custom data (max 5KB) that you can assign to:
        /// - <see cref="IStreamCall"/>
        /// - <see cref="IStreamVideoCallParticipant"/>
        /// </summary>
        IStreamCustomData CustomData { get; }
    }
}