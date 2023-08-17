using System;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;

namespace StreamVideo.Core
{
    public interface IStreamVideoClient : IDisposable
    {
        Task ConnectUserAsync(AuthCredentials credentials);

        void Update();

        Task DisconnectAsync();

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify);

        event ParticipantTrackChangedHandler TrackAdded;
    }
}