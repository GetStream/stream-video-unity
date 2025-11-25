using System;
using System.Collections.Generic;
using System.Threading;
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
        /// Called when client is disconnected. Contains the <see cref="DisconnectReason"/> for the disconnection.
        /// </summary>
        event DisconnectedHandler Disconnected;

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
        /// The client can only be in a single call at a time.
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
        IStreamVideoDeviceManager VideoDeviceManager { get; }
        
        /// <summary>
        /// Manager for audio recording devices. Use it to interact with microphone devices.
        /// </summary>
        IStreamAudioDeviceManager AudioDeviceManager { get; }

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

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets <see cref="IStreamCall"/> information without joining it. Will return null if the call doesn't exist
        /// </summary>
        Task<IStreamCall> GetCallAsync(StreamCallType callType, string callId);
        
        /// <summary>
        /// Gets <see cref="IStreamCall"/> information without joining it. Will return null if the call doesn't exist
        /// </summary>
        Task<IStreamCall> GetCallAsync(StreamCallType callType, string callId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Get a call with a specified Type and ID. If such a call doesn't exist, it will be created.
        /// </summary>
        /// <param name="callType">Call type - this defines the permissions and other settings for the call. Read more in the <a href="https://getstream.io/video/docs/unity/guides/call-types/">Call Types Docs</a></param>
        /// <param name="callId">Call ID</param>
        /// <returns>Call object of type: <see cref="IStreamCall"/></returns>
        Task<IStreamCall> GetOrCreateCallAsync(StreamCallType callType, string callId);
        
        /// <summary>
        /// Get a call with a specified Type and ID. If such a call doesn't exist, it will be created.
        /// </summary>
        /// <param name="callType">Call type - this defines the permissions and other settings for the call. Read more in the <a href="https://getstream.io/video/docs/unity/guides/call-types/">Call Types Docs</a></param>
        /// <param name="callId">Call ID</param>
        /// <returns>Call object of type: <see cref="IStreamCall"/></returns>
        Task<IStreamCall> GetOrCreateCallAsync(StreamCallType callType, string callId, CancellationToken cancellationToken);

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

#if STREAM_DEBUG_ENABLED
        Task SendDebugLogs(string callId, string participantId);
#endif
        void SetAudioProcessingModule(bool enabled, bool echoCancellationEnabled, bool autoGainEnabled, bool noiseSuppressionEnabled, int noiseSuppressionLevel);
        
        void GetAudioProcessingModuleConfig(out bool enabled, out bool echoCancellationEnabled, out bool autoGainEnabled, out bool noiseSuppressionEnabled, out int noiseSuppressionLevel);


        /// <summary>
        /// Temporary method (can be removed in the future) to pause audio playback on Android.
        /// This will completely suspend playback of any audio coming from the StreamVideo SDK on the Android platform.
        /// </summary>
        void PauseAndroidAudioPlayback();

        /// <summary>
        /// Temporary method (can be removed in the future) to resume audio playback on Android.
        /// Call this resume audio playback if it was previously paused using <see cref="PauseAndroidAudioPlayback"/>.
        /// </summary>
        void ResumeAndroidAudioPlayback();
    }
}