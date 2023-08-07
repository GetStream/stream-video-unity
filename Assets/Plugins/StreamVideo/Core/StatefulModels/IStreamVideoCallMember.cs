using StreamVideo.Core.State;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// A <see cref="IStreamVideoUser"/> that is invited to a <see cref="IStreamCall"/>.
    /// Once the member joins the call he will become a <see cref="IStreamVideoCallParticipant"/>
    /// </summary>
    public interface IStreamVideoCallMember : IStreamStatefulModel
    {
    }
}