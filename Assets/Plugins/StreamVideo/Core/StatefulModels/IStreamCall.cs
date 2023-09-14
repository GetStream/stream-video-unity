using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Models;
using StreamVideo.Core.State;

namespace StreamVideo.Core.StatefulModels
{
    public interface IStreamCall : IStreamStatefulModel
    {
        event ParticipantTrackChangedHandler TrackAdded;
        
        Credentials Credentials { get; }
        IReadOnlyList<IStreamVideoCallParticipant> Participants { get; }
        IReadOnlyList<OwnCapability> OwnCapabilities { get; }

        /// <summary>
        /// Call ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The unique identifier for a call (&lt;type&gt;:&lt;id&gt;)
        /// </summary>
        string Cid { get; }

        /// <summary>
        /// The type of call
        /// </summary>
        StreamCallType Type { get; }

        /// <summary>
        /// The user that created the call
        /// </summary>
        IStreamVideoUser CreatedBy { get; }

        bool IsLocalUserOwner { get; }

        event ParticipantJoinedHandler ParticipantJoined;
        event ParticipantLeftHandler ParticipantLeft;

        Task LeaveAsync();

        Task EndAsync();

        Task GoLiveAsync();

        Task StopLiveAsync();

        Task StartRecordingAsync();

        Task StopRecordingAsync();

        Task MuteAllUsersAsync(bool audio, bool video, bool screenShare);
    }
}