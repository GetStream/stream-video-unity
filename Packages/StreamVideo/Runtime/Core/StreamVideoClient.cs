using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.QueryBuilders.Sort.Calls;
using Stream.Video.v1.Sfu.Events;
using StreamVideo.Core.Configs;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs;
using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.VideoClientInstanceRunner;
using StreamVideo.Libs.Websockets;
using Unity.WebRTC;
using UnityEngine;
using Cache = StreamVideo.Core.State.Caches.Cache;

namespace StreamVideo.Core
{
    public delegate void CallHandler(IStreamCall call);
    public delegate void ConnectHandler(IStreamVideoUser localUser);

    public class StreamVideoClient : IStreamVideoClient
    {
        public event ConnectHandler Connected;
        
        public event CallHandler CallStarted;
        public event CallHandler CallEnded;

        public IStreamVideoUser LocalUser { get; private set; }
        public IStreamCall ActiveCall => InternalLowLevelClient.RtcSession.ActiveCall;

        public bool IsConnected => InternalLowLevelClient.ConnectionState == ConnectionState.Connected;

        /// <summary>
        /// Use this method to create the Video Client. You should have only one instance of this class
        /// </summary>
        public static IStreamVideoClient CreateDefaultClient(IStreamClientConfig config = default)
        {
            var factory = new StreamDependenciesFactory();

            config ??= StreamClientConfig.Default;
            var logs = factory.CreateLogger(config.LogLevel.ToLogLevel());
            var applicationInfo = factory.CreateApplicationInfo();
            var coordinatorWebSocket
                = factory.CreateWebsocketClient(logs, isDebugMode: config.LogLevel.IsDebugEnabled());
            var sfuWebSocket
                = factory.CreateWebsocketClient(logs, isDebugMode: config.LogLevel.IsDebugEnabled());
            var httpClient = factory.CreateHttpClient();
            var serializer = factory.CreateSerializer();
            var timeService = factory.CreateTimeService();
            var networkMonitor = factory.CreateNetworkMonitor();
            var gameObjectRunner = factory.CreateClientRunner();

            var client = new StreamVideoClient(coordinatorWebSocket, sfuWebSocket, httpClient,
                serializer, timeService, networkMonitor, applicationInfo, logs, config);
            
            gameObjectRunner?.RunClientInstance(client);
            
            return client;
        }

        /// <summary>
        /// Will return null if the call doesn't exist
        /// </summary>
        public async Task<IStreamCall> GetCallAsync(StreamCallType callType, string callId)
        {
            //StreamTodo: validate input

            var callData
                = await InternalLowLevelClient.InternalVideoClientApi.GetCallAsync(callType, callId,
                    new GetOrCreateCallRequestInternalDTO());
            return _cache.TryCreateOrUpdate(callData);
        }

        //StreamTodo: add more params (same as in JoinCallAsync) + add to interface
        public async Task<IStreamCall> GetOrCreateCallAsync(StreamCallType callType, string callId)
        {
            //StreamTodo: validate input

            var callData
                = await InternalLowLevelClient.InternalVideoClientApi.GetOrCreateCallAsync(callType, callId,
                    new GetOrCreateCallRequestInternalDTO());
            //StreamTodo: what if null?
            return _cache.TryCreateOrUpdate(callData);
        }

        //StreamTodo: if ring and notify can't be both true then perhaps enum NotifyMode.Ring, NotifyMode.Notify?
        //StreamTodo: add CreateCallOptions
        public async Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify)
        {
            //StreamTodo: check if we're already in a call?

            IStreamCall call;
            if (!create)
            {
                //StreamTodo: check android SDK if the flow is the same
                call = await GetCallAsync(callType, callId);
                if (call == null)
                {
                    throw new InvalidOperationException($"Call with id `{callId}` was not found");
                }
            }
            else
            {
                call = await GetOrCreateCallAsync(callType, callId);
            }

            // StreamTodo: check state if we don't have an active session already
            var locationHint = await InternalLowLevelClient.GetLocationHintAsync();

            //StreamTodo: move this logic to call.Join, this way user can create call object and join later on 

            // StreamTodo: expose params
            var joinCallRequest = new JoinCallRequestInternalDTO
            {
                Create = create,
                Data = new CallRequestInternalDTO
                {
                    CreatedBy = null,
                    CreatedById = null,
                    Custom = null,
                    Members = null,
                    SettingsOverride = null,
                    StartsAt = DateTimeOffset.Now,
                    Team = null
                },
                Location = locationHint,
                MembersLimit = 0,
                MigratingFrom = null,
                Notify = notify,
                Ring = ring
            };

            var joinCallResponse
                = await InternalLowLevelClient.InternalVideoClientApi.JoinCallAsync(callType, callId, joinCallRequest);
            _cache.TryCreateOrUpdate(joinCallResponse);

            await InternalLowLevelClient.StartCallSessionAsync((StreamCall)call);

            CallStarted?.Invoke(call);
            return call;
        }

        public void Dispose()
        {
            UnsubscribeFrom(InternalLowLevelClient);
            InternalLowLevelClient?.Dispose();
            Destroyed?.Invoke();
        }

        public async Task<IStreamVideoUser> ConnectUserAsync(AuthCredentials credentials)
        {
            await InternalLowLevelClient.ConnectUserAsync(credentials);
            Connected?.Invoke(LocalUser);
            return LocalUser;
        }

        public void Update() => InternalLowLevelClient.Update();

        public IEnumerator WebRTCUpdateCoroutine() => WebRTC.Update();

        public Task DisconnectAsync() => InternalLowLevelClient.DisconnectAsync();

        public void SetAudioInputSource(AudioSource audioSource)
        {
            InternalLowLevelClient.RtcSession.AudioInput = audioSource;
        }

        //StreamTodo: later we should accept just Texture or RenderTexture or TextureProvider
        public void SetCameraInputSource(WebCamTexture webCamTexture)
        {
            InternalLowLevelClient.RtcSession.VideoInput = webCamTexture;
        }

        public void SetCameraInputSource(Camera sceneCamera)
        {
            InternalLowLevelClient.RtcSession.VideoSceneInput = sceneCamera;
        }

        public async Task<QueryCallsResult> QueryCallsAsync(IEnumerable<IFieldFilterRule> filters = null, CallSort sort = null, int limit = 25, string prev = null, string next = null, bool watch = false)
        {
            var request = new QueryCallsRequestInternalDTO
            {
                FilterConditions = filters?.Select(_ => _.GenerateFilterEntry()).ToDictionary(x => x.Key, x => x.Value),
                Limit = limit,
                Next = next,
                Prev = prev,
                Sort = sort?.ToSortParamRequestList(),
                Watch = watch
            };

            var response = await InternalLowLevelClient.InternalVideoClientApi.QueryCallsAsync(request);
            if (response == null || response.Calls == null || response.Calls.Count == 0)
            {
                return new QueryCallsResult();
            }

            var calls = new List<IStreamCall>();
            foreach (var callDto in response.Calls)
            {
                _cache.TryCreateOrUpdate(callDto);
            }
            
            return new QueryCallsResult(calls, response.Prev, response.Next);
        }
        
        #region IStreamVideoClientEventsListener
        
        event Action IStreamVideoClientEventsListener.Destroyed
        {
            add => this.Destroyed += value;
            remove => this.Destroyed -= value;
        }

        void IStreamVideoClientEventsListener.Destroy()
        {
            //StreamTodo: we should probably check: if waiting for connection -> cancel, if connected -> disconnect, etc
            DisconnectAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logs.Exception(t.Exception);
                    return;
                }

                Dispose();
            });
        }
        
        #endregion

        internal StreamVideoLowLevelClient InternalLowLevelClient { get; private set; }

        internal async Task LeaveCallAsync(IStreamCall call)
        {
            //StreamTodo: check if call is active
            await InternalLowLevelClient.RtcSession.StopAsync();
            CallEnded?.Invoke(call);
        }

        internal async Task EndCallAsync(IStreamCall call)
        {
            //StreamTodo: check if call is active
            await InternalLowLevelClient.InternalVideoClientApi.EndCallAsync(call.Type, call.Id);
            await LeaveCallAsync(call);
        }
        
        internal Task StartHLSAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StartBroadcastingAsync(call.Type, call.Id);

        internal Task StopHLSAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StopBroadcastingAsync(call.Type, call.Id);

        internal Task GoLiveAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.GoLiveAsync(call.Type, call.Id);

        internal Task StopLiveAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StopLiveAsync(call.Type, call.Id);

        internal Task StartRecordingAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StartRecordingAsync(call.Type, call.Id);

        internal Task StopRecordingAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StopRecordingAsync(call.Type, call.Id);

        internal Task MuteAllUsersAsync(IStreamCall call, bool audio, bool video, bool screenShare)
        {
            var body = new MuteUsersRequestInternalDTO
            {
                Audio = audio,
                MuteAllUsers = true,
                Screenshare = screenShare,
                //UserIds = null,
                Video = video
            };
            return InternalLowLevelClient.InternalVideoClientApi.MuteUsersAsync(call.Type, call.Id, body);
        }

        internal Task BlockUserAsync(IStreamCall call, string userId)
            => InternalLowLevelClient.InternalVideoClientApi.BlockUserAsync(call.Type, call.Id,
                new BlockUserRequestInternalDTO
                {
                    UserId = userId
                });

        internal Task UnblockUserAsync(IStreamCall call, string userId)
            => InternalLowLevelClient.InternalVideoClientApi.UnblockUserAsync(call.Type, call.Id, new UnblockUserRequestInternalDTO
            {
                UserId = userId
            });

        internal Task RequestPermissionAsync(IStreamCall call, List<string> capabilities)
            => InternalLowLevelClient.InternalVideoClientApi.RequestPermissionAsync(call.Type, call.Id,
                new RequestPermissionRequestInternalDTO
                {
                    Permissions = capabilities
                });
        
        internal Task UpdateUserPermissions(IStreamCall call, string userId, List<string> grantPermissions, List<string> revokePermissions)
            => InternalLowLevelClient.InternalVideoClientApi.UpdateUserPermissionsAsync(call.Type, call.Id,
                new UpdateUserPermissionsRequestInternalDTO
                {
                    GrantPermissions = grantPermissions,
                    RevokePermissions = revokePermissions,
                    UserId = userId
                });

        internal Task RemoveMembersAsync(IStreamCall call, List<string> removeUsers)
            => InternalLowLevelClient.InternalVideoClientApi.UpdateCallMembersAsync(call.Type, call.Id,
                new UpdateCallMembersRequestInternalDTO
                {
                    RemoveMembers = removeUsers,
                });

        private event Action Destroyed;
        
        private readonly ILogs _logs;
        private readonly ICache _cache;
        
        private StreamVideoClient(IWebsocketClient coordinatorWebSocket, IWebsocketClient sfuWebSocket,
            IHttpClient httpClient, ISerializer serializer, ITimeService timeService, INetworkMonitor networkMonitor,
            IApplicationInfo applicationInfo, ILogs logs, IStreamClientConfig config)
        {
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));

            InternalLowLevelClient = new StreamVideoLowLevelClient(coordinatorWebSocket, sfuWebSocket, httpClient,
                serializer, timeService, networkMonitor, applicationInfo, logs, config);

            _cache = new Cache(this, serializer, _logs);
            InternalLowLevelClient.RtcSession.SetCache(_cache);

            SubscribeTo(InternalLowLevelClient);
        }

        private void SubscribeTo(StreamVideoLowLevelClient lowLevelClient)
        {
            lowLevelClient.InternalCallCreatedEvent += OnInternalCallCreatedEvent;
            lowLevelClient.InternalCallUpdatedEvent += OnInternalCallUpdatedEvent;
            lowLevelClient.InternalCallEndedEvent += OnInternalCallEndedEvent;
            lowLevelClient.InternalParticipantJoinedEvent += OnInternalParticipantJoinedEvent;
            lowLevelClient.InternalParticipantLeftEvent += OnInternalParticipantLeftEvent;
            lowLevelClient.InternalCallAcceptedEvent += OnInternalCallAcceptedEvent;
            lowLevelClient.InternalCallRejectedEvent += OnInternalCallRejectedEvent;
            lowLevelClient.InternalCallLiveStartedEvent += OnInternalCallLiveStartedEvent;
            lowLevelClient.InternalCallMemberAddedEvent += OnInternalCallMemberAddedEvent;
            lowLevelClient.InternalCallMemberRemovedEvent += OnInternalCallMemberRemovedEvent;
            lowLevelClient.InternalCallMemberUpdatedEvent += OnInternalCallMemberUpdatedEvent;
            lowLevelClient.InternalCallMemberUpdatedPermissionEvent += OnInternalCallMemberUpdatedPermissionEvent;
            lowLevelClient.InternalCallNotificationEvent += OnInternalCallNotificationEvent;
            lowLevelClient.InternalPermissionRequestEvent += OnInternalPermissionRequestEvent;
            lowLevelClient.InternalUpdatedCallPermissionsEvent += OnInternalUpdatedCallPermissionsEvent;
            lowLevelClient.InternalCallReactionEvent += OnInternalCallReactionEvent;
            lowLevelClient.InternalCallRecordingStartedEvent += OnInternalCallRecordingStartedEvent;
            lowLevelClient.InternalCallRecordingStoppedEvent += OnInternalCallRecordingStoppedEvent;
            lowLevelClient.InternalBlockedUserEvent += OnInternalBlockedUserEvent;
            lowLevelClient.InternalCallBroadcastingStartedEvent += OnInternalCallBroadcastingStartedEvent;
            lowLevelClient.InternalCallBroadcastingStoppedEvent += OnInternalCallBroadcastingStoppedEvent;
            lowLevelClient.InternalCallRingEvent += OnInternalCallRingEvent;
            lowLevelClient.InternalCallSessionEndedEvent += OnInternalCallSessionEndedEvent;
            lowLevelClient.InternalCallSessionStartedEvent += OnInternalCallSessionStartedEvent;
            lowLevelClient.InternalCallUnblockedUserEvent += OnInternalCallUnblockedUserEvent;
            lowLevelClient.InternalConnectionErrorEvent += OnInternalConnectionErrorEvent;
            lowLevelClient.InternalCustomVideoEvent += OnInternalCustomVideoEvent;
            
            lowLevelClient.Connected += InternalLowLevelClientOnConnected;
        }

        private void UnsubscribeFrom(StreamVideoLowLevelClient lowLevelClient)
        {
            lowLevelClient.InternalCallCreatedEvent -= OnInternalCallCreatedEvent;
            lowLevelClient.InternalCallUpdatedEvent -= OnInternalCallUpdatedEvent;
            lowLevelClient.InternalCallEndedEvent -= OnInternalCallEndedEvent;
            lowLevelClient.InternalParticipantJoinedEvent -= OnInternalParticipantJoinedEvent;
            lowLevelClient.InternalParticipantLeftEvent -= OnInternalParticipantLeftEvent;
            lowLevelClient.InternalCallAcceptedEvent -= OnInternalCallAcceptedEvent;
            lowLevelClient.InternalCallRejectedEvent -= OnInternalCallRejectedEvent;
            lowLevelClient.InternalCallLiveStartedEvent -= OnInternalCallLiveStartedEvent;
            lowLevelClient.InternalCallMemberAddedEvent -= OnInternalCallMemberAddedEvent;
            lowLevelClient.InternalCallMemberRemovedEvent -= OnInternalCallMemberRemovedEvent;
            lowLevelClient.InternalCallMemberUpdatedEvent -= OnInternalCallMemberUpdatedEvent;
            lowLevelClient.InternalCallMemberUpdatedPermissionEvent -= OnInternalCallMemberUpdatedPermissionEvent;
            lowLevelClient.InternalCallNotificationEvent -= OnInternalCallNotificationEvent;
            lowLevelClient.InternalPermissionRequestEvent -= OnInternalPermissionRequestEvent;
            lowLevelClient.InternalUpdatedCallPermissionsEvent -= OnInternalUpdatedCallPermissionsEvent;
            lowLevelClient.InternalCallReactionEvent -= OnInternalCallReactionEvent;
            lowLevelClient.InternalCallRecordingStartedEvent -= OnInternalCallRecordingStartedEvent;
            lowLevelClient.InternalCallRecordingStoppedEvent -= OnInternalCallRecordingStoppedEvent;
            lowLevelClient.InternalBlockedUserEvent -= OnInternalBlockedUserEvent;
            lowLevelClient.InternalCallBroadcastingStartedEvent -= OnInternalCallBroadcastingStartedEvent;
            lowLevelClient.InternalCallBroadcastingStoppedEvent -= OnInternalCallBroadcastingStoppedEvent;
            lowLevelClient.InternalCallRingEvent -= OnInternalCallRingEvent;
            lowLevelClient.InternalCallSessionEndedEvent -= OnInternalCallSessionEndedEvent;
            lowLevelClient.InternalCallSessionStartedEvent -= OnInternalCallSessionStartedEvent;
            lowLevelClient.InternalCallUnblockedUserEvent -= OnInternalCallUnblockedUserEvent;
            lowLevelClient.InternalConnectionErrorEvent -= OnInternalConnectionErrorEvent;
            lowLevelClient.InternalCustomVideoEvent -= OnInternalCustomVideoEvent;
            
            lowLevelClient.Connected -= InternalLowLevelClientOnConnected;
        }
        
        private void InternalLowLevelClientOnConnected()
        {
            LocalUser = _cache.TryCreateOrUpdate(InternalLowLevelClient.LocalUserDto);
        }

        private void OnInternalCallCreatedEvent(CallCreatedEventInternalDTO eventData)
        {
            // Implement handling logic for CallCreatedEventInternalDTO here
        }

        private void OnInternalCallUpdatedEvent(CallUpdatedEventInternalDTO eventData)
        {
            // Implement handling logic for CallUpdatedEventInternalDTO here
        }

        private void OnInternalCallEndedEvent(CallEndedEventInternalDTO eventData)
        {
            // Implement handling logic for CallEndedEventInternalDTO here
        }

        private void OnInternalParticipantJoinedEvent(ParticipantJoined eventData)
        {
            // Implement handling logic for ParticipantJoined here
        }

        private void OnInternalParticipantLeftEvent(ParticipantLeft eventData)
        {
            // Implement handling logic for ParticipantLeft here
        }

        private void OnInternalCallAcceptedEvent(CallAcceptedEventInternalDTO eventData)
        {
            // Implement handling logic for CallAcceptedEventInternalDTO here
        }

        private void OnInternalCallRejectedEvent(CallRejectedEventInternalDTO eventData)
        {
            // Implement handling logic for CallRejectedEventInternalDTO here
        }

        private void OnInternalCallLiveStartedEvent(CallLiveStartedEventInternalDTO eventData)
        {
            // Implement handling logic for CallLiveStartedEventInternalDTO here
        }

        private void OnInternalCallMemberAddedEvent(CallMemberAddedEventInternalDTO eventData)
        {
            // Implement handling logic for CallMemberAddedEventInternalDTO here
        }

        private void OnInternalCallMemberRemovedEvent(CallMemberRemovedEventInternalDTO eventData)
        {
            // Implement handling logic for CallMemberRemovedEventInternalDTO here
        }

        private void OnInternalCallMemberUpdatedEvent(CallMemberUpdatedEventInternalDTO eventData)
        {
            // Implement handling logic for CallMemberUpdatedEventInternalDTO here
        }

        private void OnInternalCallMemberUpdatedPermissionEvent(CallMemberUpdatedPermissionEventInternalDTO eventData)
        {
            // Implement handling logic for CallMemberUpdatedPermissionEventInternalDTO here
        }

        private void OnInternalCallNotificationEvent(CallNotificationEventInternalDTO eventData)
        {
            // Implement handling logic for CallNotificationEventInternalDTO here
        }

        private void OnInternalPermissionRequestEvent(PermissionRequestEventInternalDTO eventData)
        {
            // Implement handling logic for PermissionRequestEventInternalDTO here
        }

        private void OnInternalUpdatedCallPermissionsEvent(UpdatedCallPermissionsEventInternalDTO eventData)
        {
            // Implement handling logic for UpdatedCallPermissionsEventInternalDTO here
        }

        private void OnInternalCallReactionEvent(CallReactionEventInternalDTO eventData)
        {
            // Implement handling logic for CallReactionEventInternalDTO here
        }

        private void OnInternalCallRecordingStartedEvent(CallRecordingStartedEventInternalDTO eventData)
        {
            // Implement handling logic for CallRecordingStartedEventInternalDTO here
        }

        private void OnInternalCallRecordingStoppedEvent(CallRecordingStoppedEventInternalDTO eventData)
        {
            // Implement handling logic for CallRecordingStoppedEventInternalDTO here
        }

        private void OnInternalBlockedUserEvent(BlockedUserEventInternalDTO eventData)
        {
            // Implement handling logic for BlockedUserEventInternalDTO here
        }

        private void OnInternalCallBroadcastingStartedEvent(CallBroadcastingStartedEventInternalDTO eventData)
        {
            // Implement handling logic for CallBroadcastingStartedEventInternalDTO here
        }

        private void OnInternalCallBroadcastingStoppedEvent(CallBroadcastingStoppedEventInternalDTO eventData)
        {
            // Implement handling logic for CallBroadcastingStoppedEventInternalDTO here
        }

        private void OnInternalCallRingEvent(CallRingEventInternalDTO eventData)
        {
            // Implement handling logic for CallRingEventInternalDTO here
        }

        private void OnInternalCallSessionEndedEvent(CallSessionEndedEventInternalDTO eventData)
        {
            // Implement handling logic for CallSessionEndedEventInternalDTO here
        }

        private void OnInternalCallSessionStartedEvent(CallSessionStartedEventInternalDTO eventData)
        {
            // Implement handling logic for CallSessionStartedEventInternalDTO here
        }

        private void OnInternalCallUnblockedUserEvent(BlockedUserEventInternalDTO eventData)
        {
            // Implement handling logic for CallUnblockedUserEventInternalDTO here
        }

        private void OnInternalConnectionErrorEvent(ConnectionErrorEventInternalDTO eventData)
        {
            // Implement handling logic for ConnectionErrorEventInternalDTO here
        }

        private void OnInternalCustomVideoEvent(CustomVideoEventInternalDTO eventData)
        {
            // Implement handling logic for CustomVideoEventInternalDTO here
        }

    }
}