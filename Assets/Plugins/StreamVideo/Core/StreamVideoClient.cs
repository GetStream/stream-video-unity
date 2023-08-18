using System;
using System.Threading.Tasks;
using Stream.Video.v1.Sfu.Events;
using StreamVideo.Core.Configs;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
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
using StreamVideo.Libs.Websockets;
using Cache = StreamVideo.Core.State.Caches.Cache;

namespace StreamVideo.Core
{
    public class StreamVideoClient : IStreamVideoClient
    {
        /// <summary>
        /// Use this method to create the main client instance
        /// </summary>
        /// <param name="authCredentials">Authorization data with ApiKey, UserToken and UserId</param>
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

            return new StreamVideoClient(coordinatorWebSocket, sfuWebSocket, httpClient,
                serializer, timeService, networkMonitor, applicationInfo, logs, config);
        }
        
        /// <summary>
        /// Will return null if the call doesn't exist
        /// </summary>
        public async Task<IStreamCall> GetCallAsync(StreamCallType callType, string callId)
        {
            //StreamTodo: validate input
            
            var callData = await InternalLowLevelClient.InternalVideoClientApi.GetCallAsync(callType, callId, new GetOrCreateCallRequestInternalDTO());
            //StreamTodo: what if null? should we fail?
            return _cache.TryCreateOrUpdate(callData);
        }

        //StreamTodo: add more params (same as in JoinCallAsync) + add to interface
        public async Task<IStreamCall> GetOrCreateCallAsync(StreamCallType callType, string callId)
        {
            //StreamTodo: validate input
            
            var callData = await InternalLowLevelClient.InternalVideoClientApi.GetOrCreateCallAsync(callType, callId, new GetOrCreateCallRequestInternalDTO());
            //StreamTodo: what if null?
            return _cache.TryCreateOrUpdate(callData);
        }

        //StreamTodo: if ring and notify can't be both true then perhaps enum NotifyMode.Ring, NotifyMode.Notify?
        //StreamTodo: add CreateCallOptions
        public async Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify)
        {
            var dto = new CallResponseInternalDTO
            {
                Backstage = false,
                BlockedUserIds = null,
                Cid = $"{callType}:{callId}",
                CreatedAt = default,
                CreatedBy = null,
                CurrentSessionId = null,
                Custom = null,
                Egress = null,
                EndedAt = default,
                Id = callId,
                Ingress = null,
                Recording = false,
                Session = null,
                Settings = null,
                StartsAt = default,
                Team = null,
                Transcribing = false,
                Type = callType,
                UpdatedAt = default
            };

            IStreamCall call = null;
            if (!create)
            {
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

            var joinCallResponse = await InternalLowLevelClient.InternalVideoClientApi.JoinCallAsync(callType, callId, joinCallRequest);
            _cache.TryCreateOrUpdate(joinCallResponse);
            
            await InternalLowLevelClient.StartCallSessionAsync((StreamCall)call);

            return call;
        }
        
        public void Dispose()
        {
            UnsubscribeFrom(InternalLowLevelClient);
            InternalLowLevelClient?.Dispose();
        }

        public Task ConnectUserAsync(AuthCredentials credentials)
            => InternalLowLevelClient.ConnectUserAsync(credentials);

        //StreamTodo: hide this and have it called by hidden runner
        public void Update() => InternalLowLevelClient.Update();

        public Task DisconnectAsync() => InternalLowLevelClient.DisconnectAsync();

        internal StreamVideoLowLevelClient InternalLowLevelClient { get; private set; }

        private StreamVideoClient(IWebsocketClient coordinatorWebSocket, IWebsocketClient sfuWebSocket,
            IHttpClient httpClient, ISerializer serializer, ITimeService timeService, INetworkMonitor networkMonitor,
            IApplicationInfo applicationInfo, ILogs logs, IStreamClientConfig config)
        {
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));

            InternalLowLevelClient = new StreamVideoLowLevelClient(coordinatorWebSocket, sfuWebSocket, httpClient,
                serializer, timeService, networkMonitor, applicationInfo, logs, config);

            _cache = new Cache(this, serializer, _logs);

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
        }

        private readonly ILogs _logs;
        private readonly ITimeService _timeService;
        private readonly ICache _cache;

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