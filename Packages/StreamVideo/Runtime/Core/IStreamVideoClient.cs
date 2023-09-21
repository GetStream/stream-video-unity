using System;
using System.Threading.Tasks;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using UnityEngine;

namespace StreamVideo.Core
{
    public interface IStreamVideoClient : IDisposable
    {
        /// <summary>
        /// Called when client is connected. Returns local user object of type <see cref="IStreamVideoUser"/>
        /// </summary>
        event ConnectHandler Connected;

        event CallHandler CallStarted;
        event CallHandler CallEnded;
        IStreamCall ActiveCall { get; }

        /// <summary>
        /// Connect user to Stream server. Returns local user object of type <see cref="IStreamVideoUser"/>
        /// </summary>
        /// <param name="credentials">Credentials required to connect user: api_key, user_id, and user_token</param>
        Task<IStreamVideoUser> ConnectUserAsync(AuthCredentials credentials);

        void Update();

        Task DisconnectAsync();

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify);

        void SetAudioInputSource(AudioSource audioSource);

        void SetCameraInputSource(WebCamTexture webCamTexture);

        void SetCameraInputSource(Camera sceneCamera);
    }
}