using System;
using System.Threading.Tasks;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using UnityEngine;

namespace StreamVideo.Core
{
    public interface IStreamVideoClient : IDisposable
    {
        event Action<Texture> VideoReceived;
        
        Task ConnectUserAsync(AuthCredentials credentials);

        void Update();

        Task DisconnectAsync();

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify);
    }
}