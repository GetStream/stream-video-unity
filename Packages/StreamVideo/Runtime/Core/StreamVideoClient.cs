using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.QueryBuilders.Sort.Calls;
using StreamVideo.Core.Configs;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;
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
using StreamVideo.Libs.Utils;
using StreamVideo.Libs.VideoClientInstanceRunner;
using StreamVideo.Libs.Websockets;
using Unity.WebRTC;
using UnityEngine;
using Cache = StreamVideo.Core.State.Caches.Cache;

namespace StreamVideo.Core
{
    public delegate void CallHandler(IStreamCall call);

    public delegate void ConnectHandler(IStreamVideoUser localUser);

    public class StreamVideoClient : IStreamVideoClient, IInternalStreamVideoClient
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

        //StreamTodo: change public to explicit interface
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

        public async Task<QueryCallsResult> QueryCallsAsync(IEnumerable<IFieldFilterRule> filters = null,
            CallSort sort = null, int limit = 25, string prev = null, string next = null, bool watch = false)
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


        StreamVideoLowLevelClient IInternalStreamVideoClient.InternalLowLevelClient => InternalLowLevelClient;

        Task IInternalStreamVideoClient.LeaveCallAsync(IStreamCall call) => LeaveCallAsync(call);

        async Task IInternalStreamVideoClient.EndCallAsync(IStreamCall call)
        {
            //StreamTodo: check if call is active
            await InternalLowLevelClient.InternalVideoClientApi.EndCallAsync(call.Type, call.Id);
            await LeaveCallAsync(call);
        }

        Task IInternalStreamVideoClient.StartHLSAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StartBroadcastingAsync(call.Type, call.Id);

        Task IInternalStreamVideoClient.StopHLSAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StopBroadcastingAsync(call.Type, call.Id);

        Task IInternalStreamVideoClient.GoLiveAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.GoLiveAsync(call.Type, call.Id);

        Task IInternalStreamVideoClient.StopLiveAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StopLiveAsync(call.Type, call.Id);

        Task IInternalStreamVideoClient.StartRecordingAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StartRecordingAsync(call.Type, call.Id);

        Task IInternalStreamVideoClient.StopRecordingAsync(IStreamCall call)
            => InternalLowLevelClient.InternalVideoClientApi.StopRecordingAsync(call.Type, call.Id);

        Task IInternalStreamVideoClient.MuteAllUsersAsync(IStreamCall call, bool audio, bool video, bool screenShare)
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

        Task IInternalStreamVideoClient.BlockUserAsync(IStreamCall call, string userId)
            => InternalLowLevelClient.InternalVideoClientApi.BlockUserAsync(call.Type, call.Id,
                new BlockUserRequestInternalDTO
                {
                    UserId = userId
                });

        Task IInternalStreamVideoClient.UnblockUserAsync(IStreamCall call, string userId)
            => InternalLowLevelClient.InternalVideoClientApi.UnblockUserAsync(call.Type, call.Id,
                new UnblockUserRequestInternalDTO
                {
                    UserId = userId
                });

        Task IInternalStreamVideoClient.RequestPermissionAsync(IStreamCall call, List<string> capabilities)
            => InternalLowLevelClient.InternalVideoClientApi.RequestPermissionAsync(call.Type, call.Id,
                new RequestPermissionRequestInternalDTO
                {
                    Permissions = capabilities
                });

        Task IInternalStreamVideoClient.UpdateUserPermissions(IStreamCall call, string userId,
            List<string> grantPermissions,
            List<string> revokePermissions)
            => InternalLowLevelClient.InternalVideoClientApi.UpdateUserPermissionsAsync(call.Type, call.Id,
                new UpdateUserPermissionsRequestInternalDTO
                {
                    GrantPermissions = grantPermissions,
                    RevokePermissions = revokePermissions,
                    UserId = userId
                });

        Task IInternalStreamVideoClient.RemoveMembersAsync(IStreamCall call, List<string> removeUsers)
            => InternalLowLevelClient.InternalVideoClientApi.UpdateCallMembersAsync(call.Type, call.Id,
                new UpdateCallMembersRequestInternalDTO
                {
                    RemoveMembers = removeUsers,
                });

        Task IInternalStreamVideoClient.SetParticipantCustomDataAsync(IStreamVideoCallParticipant participant,
            Dictionary<string, object> internalCustomData)
        {
            var activeCall = InternalLowLevelClient.RtcSession.ActiveCall;
            if (activeCall == null)
            {
                throw new InvalidOperationException(
                    "Tried to set custom data for a participant but there is no active call session.");
            }

            return activeCall.SyncParticipantCustomDataAsync(participant, internalCustomData);
        }

        private StreamVideoLowLevelClient InternalLowLevelClient { get; }

        private event Action Destroyed;

        private readonly ILogs _logs;
        private readonly ICache _cache;

        private async Task LeaveCallAsync(IStreamCall call)
        {
            //StreamTodo: check if call is active
            await InternalLowLevelClient.RtcSession.StopAsync();
            CallEnded?.Invoke(call);
        }

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
            lowLevelClient.InternalCallUnblockedUserEvent += OnInternalCallUnblockedUserEvent;
            lowLevelClient.InternalCallBroadcastingStartedEvent += OnInternalCallBroadcastingStartedEvent;
            lowLevelClient.InternalCallBroadcastingStoppedEvent += OnInternalCallBroadcastingStoppedEvent;
            lowLevelClient.InternalCallRingEvent += OnInternalCallRingEvent;
            lowLevelClient.InternalCallSessionEndedEvent += OnInternalCallSessionEndedEvent;
            lowLevelClient.InternalCallSessionStartedEvent += OnInternalCallSessionStartedEvent;
            lowLevelClient.InternalConnectionErrorEvent += OnInternalConnectionErrorEvent;
            lowLevelClient.InternalCustomVideoEvent += OnInternalCustomVideoEvent;

            lowLevelClient.Connected += InternalLowLevelClientOnConnected;
        }

        private void UnsubscribeFrom(StreamVideoLowLevelClient lowLevelClient)
        {
            lowLevelClient.InternalCallCreatedEvent -= OnInternalCallCreatedEvent;
            lowLevelClient.InternalCallUpdatedEvent -= OnInternalCallUpdatedEvent;
            lowLevelClient.InternalCallEndedEvent -= OnInternalCallEndedEvent;
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
            lowLevelClient.InternalCallUnblockedUserEvent -= OnInternalCallUnblockedUserEvent;
            lowLevelClient.InternalCallBroadcastingStartedEvent -= OnInternalCallBroadcastingStartedEvent;
            lowLevelClient.InternalCallBroadcastingStoppedEvent -= OnInternalCallBroadcastingStoppedEvent;
            lowLevelClient.InternalCallRingEvent -= OnInternalCallRingEvent;
            lowLevelClient.InternalCallSessionEndedEvent -= OnInternalCallSessionEndedEvent;
            lowLevelClient.InternalCallSessionStartedEvent -= OnInternalCallSessionStartedEvent;
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
            // CallCreatedEventInternalDTO desc says we should check the ringing state but this seems obsolete - there's no such property. Also, there is a CallRingEventInternalDTO
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.UpdateMembersFromDto(eventData);
        }

        private void OnInternalCallUpdatedEvent(CallUpdatedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.UpdateCapabilitiesByRoleFromDto(eventData);
        }

        private void OnInternalCallEndedEvent(CallEndedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.LeaveAsync().LogIfFailed();
        }

        private void OnInternalCallAcceptedEvent(CallAcceptedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            //StreamTodo: handle eventData.User - The user who accepted the call

            //StreamTodo: handle call accepted - check Android -> it does auto-join + updates ringing state + keeps accepted in state. We also might want to expose an event
        }

        private void OnInternalCallRejectedEvent(CallRejectedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            //StreamTodo: handle eventData.User - The user who rejected the call

            //StreamTodo: handle call rejected - check Android -> it updates ringing state + keeps rejectedBy in state. We also might want to expose an event
        }

        private void OnInternalCallLiveStartedEvent(CallLiveStartedEventInternalDTO eventData)
        {
            _cache.TryCreateOrUpdate(eventData.Call);

            //StreamTodo: expose an event that the call got started?
        }

        private void OnInternalCallMemberAddedEvent(CallMemberAddedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.UpdateMembersFromDto(eventData);
        }

        private void OnInternalCallMemberRemovedEvent(CallMemberRemovedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.UpdateMembersFromDto(eventData);
        }

        private void OnInternalCallMemberUpdatedEvent(CallMemberUpdatedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.UpdateMembersFromDto(eventData);
        }

        private void OnInternalCallMemberUpdatedPermissionEvent(CallMemberUpdatedPermissionEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.UpdateMembersFromDto(eventData);
            call.UpdateCapabilitiesByRoleFromDto(eventData);
        }

        private void OnInternalCallNotificationEvent(CallNotificationEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            call.UpdateMembersFromDto(eventData);

            //StreamTodo: handle eventData.User
        }

        private void OnInternalPermissionRequestEvent(PermissionRequestEventInternalDTO eventData)
        {
            var requestingUser = _cache.TryCreateOrUpdate(eventData.User);

            //StreamTodo: implement event PermissionsRequested + should we cast string to some enum?

            /* Android keeps permission requests in this format
             * data class PermissionRequest(
    val call: Call,
    val user: User,
    val createdAt: OffsetDateTime,
    val permissions: List<String>,
    var grantedAt: OffsetDateTime? = null,
    var rejectedAt: OffsetDateTime? = null,
)
             */
        }

        private void OnInternalUpdatedCallPermissionsEvent(UpdatedCallPermissionsEventInternalDTO eventData)
        {
            if (!AssertCidMatch(eventData.CallCid, ActiveCall?.Cid))
            {
                return;
            }

            InternalLowLevelClient.RtcSession.ActiveCall.UpdateOwnCapabilitiesFrom(eventData);
        }

        private void OnInternalCallReactionEvent(CallReactionEventInternalDTO eventData)
        {
            if (!AssertCidMatch(eventData.CallCid, ActiveCall?.Cid))
            {
                return;
            }

            InternalLowLevelClient.RtcSession.ActiveCall.InternalHandleCallRecordingStartedEvent(eventData);
        }

        private void OnInternalCallRecordingStartedEvent(CallRecordingStartedEventInternalDTO eventData)
        {
            if (!AssertCidMatch(eventData.CallCid, ActiveCall?.Cid))
            {
                return;
            }

            InternalLowLevelClient.RtcSession.ActiveCall.InternalHandleCallRecordingStartedEvent(eventData);
        }

        private void OnInternalCallRecordingStoppedEvent(CallRecordingStoppedEventInternalDTO eventData)
        {
            if (!AssertCidMatch(eventData.CallCid, ActiveCall?.Cid))
            {
                return;
            }

            InternalLowLevelClient.RtcSession.ActiveCall.InternalHandleCallRecordingStoppedEvent(eventData);
        }

        private void OnInternalBlockedUserEvent(BlockedUserEventInternalDTO eventData)
        {
            // Implement handling logic for BlockedUserEventInternalDTO here
            var blockedUser = _cache.TryCreateOrUpdate(eventData.User);
            var blockedByUser = _cache.TryCreateOrUpdate(eventData.BlockedByUser);

            //StreamTodo: expose UserBlocked event?
        }

        private void OnInternalCallUnblockedUserEvent(BlockedUserEventInternalDTO eventData)
        {
            // Implement handling logic for CallUnblockedUserEventInternalDTO here
        }

        private void OnInternalCallBroadcastingStartedEvent(CallBroadcastingStartedEventInternalDTO eventData)
        {
            //StreamTodo: Implement handling logic for CallBroadcastingStartedEventInternalDTO here
        }

        private void OnInternalCallBroadcastingStoppedEvent(CallBroadcastingStoppedEventInternalDTO eventData)
        {
            //StreamTodo: Implement handling logic for CallBroadcastingStoppedEventInternalDTO here
        }

        private void OnInternalCallRingEvent(CallRingEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            var caller = _cache.TryCreateOrUpdate(eventData.User);

            call.UpdateMembersFromDto(eventData);

            //StreamTodo: expose CallRinging event?
        }

        private void OnInternalCallSessionEndedEvent(CallSessionEndedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
            //StreamTodo: should we do anything else? 
        }

        private void OnInternalCallSessionStartedEvent(CallSessionStartedEventInternalDTO eventData)
        {
            var call = _cache.TryCreateOrUpdate(eventData.Call);
        }

        private void OnInternalConnectionErrorEvent(ConnectionErrorEventInternalDTO eventData)
        {
            // Implement handling logic for ConnectionErrorEventInternalDTO here
        }

        private void OnInternalCustomVideoEvent(CustomVideoEventInternalDTO eventData)
        {
            //StreamTodo: Implement handling logic for CustomVideoEventInternalDTO here
        }

        private bool AssertCidMatch(string cidA, string cidB)
        {
            var areEqual = cidA == cidB;
#if STREAM_DEBUG_ENABLED
            if (!areEqual)
            {
                _logs.Error($"CID mismatch: {cidA} vs {cidB}");
            }
#endif
            return areEqual;
        }
    }
}