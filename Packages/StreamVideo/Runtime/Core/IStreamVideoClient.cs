using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.QueryBuilders.Sort.Calls;
using StreamVideo.Core.QueryBuilders.Filters;
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

        /// <summary>
        /// Will return null if the call doesn't exist
        /// </summary>
        Task<IStreamCall> GetCallAsync(StreamCallType callType, string callId);

        Task<IStreamCall> GetOrCreateCallAsync(StreamCallType callType, string callId);

        /// <summary>
        /// Query calls
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="sort"></param>
        /// <param name="limit"></param>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <param name="watch">Start receiving updates. If false, the returned <see cref="IStreamCall"/> objects will not be updated</param>
        Task<QueryCallsResult> QueryCallsAsync(IEnumerable<IFieldFilterRule> filters = null, CallSort sort = null, int limit = 25, string prev = null, string next = null, bool watch = false);
    }
}