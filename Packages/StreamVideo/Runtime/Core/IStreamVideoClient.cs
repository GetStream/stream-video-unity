using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.QueryBuilders.Sort.Calls;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.VideoClientInstanceRunner;
using UnityEngine;

namespace StreamVideo.Core
{
    /// <summary>
    /// This is the main client to connect to Stream service and create calls that enable participants to share video & audio streams.
    /// </summary>
    public interface IStreamVideoClient : IStreamVideoClientEventsListener, IDisposable
    {
        /// <summary>
        /// Called when client is connected. Returns local user object of type <see cref="IStreamVideoUser"/>
        /// </summary>
        event ConnectHandler Connected;

        event CallHandler CallStarted;
        event CallHandler CallEnded;
        IStreamCall ActiveCall { get; }
        IStreamVideoUser LocalUser { get; }
        bool IsConnected { get; }

        /// <summary>
        /// Connect user to Stream server. Returns local user object of type <see cref="IStreamVideoUser"/>
        /// </summary>
        /// <param name="credentials">Credentials required to connect user: api_key, user_id, and user_token</param>
        Task<IStreamVideoUser> ConnectUserAsync(AuthCredentials credentials);

        Task DisconnectAsync();

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify);

        /// <summary>
        /// Set the source for sending AUDIO. Check out the docs to learn on how to capture audio from a Microphone device https://getstream.io/video/docs/unity/guides/camera-and-microphone/
        /// </summary>
        /// <param name="audioSource"></param>
        void SetAudioInputSource(AudioSource audioSource);

        /// <summary>
        /// Set the source for sending VIDEO from a Camera device. Check out the docs to learn how to setup capturing video from a camera device https://getstream.io/video/docs/unity/guides/camera-and-microphone/
        /// </summary>
        /// <param name="webCamTexture"></param>
        void SetCameraInputSource(WebCamTexture webCamTexture);

        /// <summary>
        /// Set the source for sending VIDEO or rendered Scene Camera. You can pass any scene camera and the video will be sent to other participants.
        /// </summary>
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