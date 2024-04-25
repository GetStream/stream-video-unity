using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.QueryBuilders.Sort.Calls;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.VideoClientInstanceRunner;

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

        /// <summary>
        /// Event fired when a call started.
        /// </summary>
        event CallHandler CallStarted;
        
        /// <summary>
        /// Event fired when a call ended
        /// </summary>
        event CallHandler CallEnded;
        
        /// <summary>
        /// Currently ongoing call session. This will be NULL if there's no call active.
        /// You can subscribe to <see cref="CallStarted"/> and <see cref="CallEnded"/> events to get notified when a call is started/ended.
        /// </summary>
        IStreamCall ActiveCall { get; }
        
        /// <summary>
        /// Object representing locally connected user
        /// </summary>
        IStreamVideoUser LocalUser { get; }
        
        /// <summary>
        /// Is user currently connected to the Stream server.
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Manager for video recording devices. Use it to interact with camera devices.
        /// </summary>
        IVideoDeviceManager VideoDeviceManager { get; }
        
        /// <summary>
        /// Manager for audio recording devices. Use it to interact with microphone devices.
        /// </summary>
        IAudioDeviceManager AudioDeviceManager { get; }

        /// <summary>
        /// Connect user to Stream server. Returns local user object of type <see cref="IStreamVideoUser"/>
        /// </summary>
        /// <param name="credentials">Credentials required to connect user: api_key, user_id, and user_token</param>
        Task<IStreamVideoUser> ConnectUserAsync(AuthCredentials credentials);

        /// <summary>
        /// Disconnect user from Stream server.
        /// </summary>
        Task DisconnectAsync();

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify);

        /// <summary>
        /// Will return null if the call doesn't exist
        /// </summary>
        Task<IStreamCall> GetCallAsync(StreamCallType callType, string callId);

        /// <summary>
        /// Get a call with a specified Type and ID. If such a call doesn't exist, it will be created.
        /// </summary>
        /// <param name="callType">Call type - this defines the permissions and other settings for the call. Read more in the <a href="https://getstream.io/video/docs/unity/guides/call-types/">Call Types Docs</a></param>
        /// <param name="callId">Call ID</param>
        /// <returns>Call object of type: <see cref="IStreamCall"/></returns>
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