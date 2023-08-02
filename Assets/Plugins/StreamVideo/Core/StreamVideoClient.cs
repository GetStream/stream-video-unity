using System;
using System.Threading.Tasks;
using Stream.Video.v1.Sfu.Events;
using StreamVideo.Core.Configs;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.LowLevelClient.API.Internal;
using StreamVideo.Core.State.Caches;
using StreamVideo.Libs;
using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;

namespace StreamVideo.Core
{
    public interface IStreamVideoClient : IDisposable
    {
        Task ConnectUserAsync(AuthCredentials credentials);

        void Update();

        Task DisconnectAsync();

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify);
    }

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
        
        //StreamTodo: if ring and notify can't be both true then perhaps enum NotifyMode.Ring, NotifyMode.Notify?
        //StreamTodo: add CreateCallOptions
        public async Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify)
        {
            var dto = new CallResponse
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

            var call = _cache.Calls.CreateOrUpdate<StreamCall, CallResponse>(dto, out _);
            if (!create)
            {
                var callData = await InternalLowLevelClient.InternalVideoClientApi.GetCallAsync(callType, callId, new GetOrCreateCallRequest());

                if (callData == null)
                {
                    //StreamTodo: error call not found
                }
                
                
                //StreamTodo: load data from response to call
            }

            // StreamTodo: check state if we don't have an active session already
            var locationHint = await InternalLowLevelClient.GetLocationHintAsync();
            
            //StreamTodo: move this logic to call.Join, this way user can create call object and join later on 

            // StreamTodo: expose params
            var joinCallRequest = new JoinCallRequest
            {
                Create = create,
                Data = new CallRequest
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
                MembersLimit = 10,
                MigratingFrom = null,
                Notify = notify,
                Ring = ring
            };

            var joinCallResponse = await InternalLowLevelClient.InternalVideoClientApi.JoinCallAsync(callType, callId, joinCallRequest);
            await InternalLowLevelClient.StartCallSessionAsync(joinCallResponse);

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

        private void OnInternalCallCreatedEvent(CallCreatedEvent eventData)
        {
            // Implement handling logic for CallCreatedEvent here
        }

        private void OnInternalCallUpdatedEvent(CallUpdatedEvent eventData)
        {
            // Implement handling logic for CallUpdatedEvent here
        }

        private void OnInternalCallEndedEvent(CallEndedEvent eventData)
        {
            // Implement handling logic for CallEndedEvent here
        }

        private void OnInternalParticipantJoinedEvent(ParticipantJoined eventData)
        {
            // Implement handling logic for ParticipantJoined here
        }

        private void OnInternalParticipantLeftEvent(ParticipantLeft eventData)
        {
            // Implement handling logic for ParticipantLeft here
        }

        private void OnInternalCallAcceptedEvent(CallAcceptedEvent eventData)
        {
            // Implement handling logic for CallAcceptedEvent here
        }

        private void OnInternalCallRejectedEvent(CallRejectedEvent eventData)
        {
            // Implement handling logic for CallRejectedEvent here
        }

        private void OnInternalCallLiveStartedEvent(CallLiveStartedEvent eventData)
        {
            // Implement handling logic for CallLiveStartedEvent here
        }

        private void OnInternalCallMemberAddedEvent(CallMemberAddedEvent eventData)
        {
            // Implement handling logic for CallMemberAddedEvent here
        }

        private void OnInternalCallMemberRemovedEvent(CallMemberRemovedEvent eventData)
        {
            // Implement handling logic for CallMemberRemovedEvent here
        }

        private void OnInternalCallMemberUpdatedEvent(CallMemberUpdatedEvent eventData)
        {
            // Implement handling logic for CallMemberUpdatedEvent here
        }

        private void OnInternalCallMemberUpdatedPermissionEvent(CallMemberUpdatedPermissionEvent eventData)
        {
            // Implement handling logic for CallMemberUpdatedPermissionEvent here
        }

        private void OnInternalCallNotificationEvent(CallNotificationEvent eventData)
        {
            // Implement handling logic for CallNotificationEvent here
        }

        private void OnInternalPermissionRequestEvent(PermissionRequestEvent eventData)
        {
            // Implement handling logic for PermissionRequestEvent here
        }

        private void OnInternalUpdatedCallPermissionsEvent(UpdatedCallPermissionsEvent eventData)
        {
            // Implement handling logic for UpdatedCallPermissionsEvent here
        }

        private void OnInternalCallReactionEvent(CallReactionEvent eventData)
        {
            // Implement handling logic for CallReactionEvent here
        }

        private void OnInternalCallRecordingStartedEvent(CallRecordingStartedEvent eventData)
        {
            // Implement handling logic for CallRecordingStartedEvent here
        }

        private void OnInternalCallRecordingStoppedEvent(CallRecordingStoppedEvent eventData)
        {
            // Implement handling logic for CallRecordingStoppedEvent here
        }

        private void OnInternalBlockedUserEvent(BlockedUserEvent eventData)
        {
            // Implement handling logic for BlockedUserEvent here
        }

        private void OnInternalCallBroadcastingStartedEvent(CallBroadcastingStartedEvent eventData)
        {
            // Implement handling logic for CallBroadcastingStartedEvent here
        }

        private void OnInternalCallBroadcastingStoppedEvent(CallBroadcastingStoppedEvent eventData)
        {
            // Implement handling logic for CallBroadcastingStoppedEvent here
        }

        private void OnInternalCallRingEvent(CallRingEvent eventData)
        {
            // Implement handling logic for CallRingEvent here
        }

        private void OnInternalCallSessionEndedEvent(CallSessionEndedEvent eventData)
        {
            // Implement handling logic for CallSessionEndedEvent here
        }

        private void OnInternalCallSessionStartedEvent(CallSessionStartedEvent eventData)
        {
            // Implement handling logic for CallSessionStartedEvent here
        }

        private void OnInternalCallUnblockedUserEvent(BlockedUserEvent eventData)
        {
            // Implement handling logic for CallUnblockedUserEvent here
        }

        private void OnInternalConnectionErrorEvent(ConnectionErrorEvent eventData)
        {
            // Implement handling logic for ConnectionErrorEvent here
        }

        private void OnInternalCustomVideoEvent(CustomVideoEvent eventData)
        {
            // Implement handling logic for CustomVideoEvent here
        }


    }
}