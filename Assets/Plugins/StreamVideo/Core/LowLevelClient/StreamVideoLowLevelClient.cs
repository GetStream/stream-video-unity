using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Stream.Video.v1.Sfu.Events;
using StreamVideo.Core.Configs;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.Auth;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient.API.Internal;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.Web;
using StreamVideo.Libs;
using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Utils;
using StreamVideo.Libs.Websockets;
using Unity.VisualScripting.Antlr3.Runtime;
#if STREAM_TESTS_ENABLED
using System.Runtime.CompilerServices;
#endif

#if STREAM_TESTS_ENABLED
[assembly: InternalsVisibleTo("StreamChat.Tests")] //StreamTodo: verify which Unity version introduced this
#endif

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Stream Chat Client - maintains WebSockets connection, executes API calls and exposes Stream events to which you can subscribe.
    /// There should be only one instance of this client in your application.
    /// </summary>
    public class StreamVideoLowLevelClient : IStreamVideoLowLevelClient
    {
        public const string MenuPrefix = "Stream/";

        //StreamTodo: make these all private 
        private static readonly Uri ServerBaseUrl = new Uri("wss://video.stream-io-api.com/video/connect");
        private static readonly Uri LocationHintWebUri = new Uri("https://hint.stream-io-video.com/");

        private const string LocationHintHeaderKey = "x-amz-cf-pop";

        public event ConnectionHandler Connected;
        public event Action Reconnecting;
        public event Action Disconnected;
        public event ConnectionStateChangeHandler ConnectionStateChanged;

        // public IChannelApi ChannelApi { get; }
        // public IMessageApi MessageApi { get; }
        // public IModerationApi ModerationApi { get; }
        // public IUserApi UserApi { get; }
        // public IDeviceApi DeviceApi { get; }

        // [Obsolete(
        //     "This property presents only initial state of the LocalUser when connection is made and is not ever updated. " +
        //     "Please use the OwnUser object returned from StreamChatClient.Connected event. This property will  be removed in the future.")]
        // public OwnUser LocalUser { get; private set; }

        public ConnectionState ConnectionState => _coordinatorWS.ConnectionState;

        //StreamTodo: wrap all params in a ReconnectPolicy object
        // public ReconnectStrategy ReconnectStrategy => _reconnectScheduler.ReconnectStrategy;
        // public float ReconnectConstantInterval => _reconnectScheduler.ReconnectConstantInterval;
        // public float ReconnectExponentialMinInterval => _reconnectScheduler.ReconnectExponentialMinInterval;
        // public float ReconnectExponentialMaxInterval => _reconnectScheduler.ReconnectExponentialMaxInterval;
        // public int ReconnectMaxInstantTrials => _reconnectScheduler.ReconnectMaxInstantTrials;
        // public double? NextReconnectTime => _reconnectScheduler.NextReconnectTime;

        /// <summary>
        /// SDK Version number
        /// </summary>
        public static readonly Version SDKVersion = new Version(0, 1, 0);

        /// <summary>
        /// Use this method to create the main client instance or use StreamChatClient constructor to create a client instance with custom dependencies
        /// </summary>
        /// <param name="authCredentials">Authorization data with ApiKey, UserToken and UserId</param>
        public static IStreamVideoLowLevelClient CreateDefaultClient(IStreamClientConfig config = default)
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

            return new StreamVideoLowLevelClient(coordinatorWebSocket, sfuWebSocket, httpClient,
                serializer,
                timeService, networkMonitor, applicationInfo, logs, config);
        }

        /// <summary>
        /// Create Development Authorization Token. Dev tokens work only if you enable "Disable Auth Checks" in your project's Dashboard.
        /// Dev tokens bypasses authorization and should only be used during development and never in production!
        /// More info <see cref="https://getstream.io/chat/docs/unity/tokens_and_authentication/?language=unity#developer-tokens"/>
        /// </summary>
        public static string CreateDeveloperAuthToken(string userId)
        {
            if (!IsUserIdValid(userId))
            {
                throw new ArgumentException($"{nameof(userId)} can only contain: a-z, 0-9, @, _ and - ");
            }

            var header = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"; //  header content = {"alg": "HS256", "typ": "JWT"}
            var devSignature = "devToken";

            var payloadBytes = Encoding.UTF8.GetBytes("{\"user_id\":\"" + userId + "\"}");
            var payload = Base64UrlEncode(payloadBytes);
            return $"{header}.{payload}.{devSignature}";
        }

        /// <summary>
        /// Strip invalid characters from a given Stream user id. The only allowed characters are: a-z, 0-9, @, _ and -
        /// </summary>
        public static string SanitizeUserId(string userId)
        {
            if (IsUserIdValid(userId))
            {
                return userId;
            }

            return Regex.Replace(userId, @"[^\w\.@_-]", "", RegexOptions.None, TimeSpan.FromSeconds(1));
        }

        public StreamVideoLowLevelClient(IWebsocketClient coordinatorWebSocket,
            IWebsocketClient sfuWebSocket,
            IHttpClient httpClient, ISerializer serializer, ITimeService timeService, INetworkMonitor networkMonitor,
            IApplicationInfo applicationInfo, ILogs logs, IStreamClientConfig config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
            _applicationInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo));
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _logs.Prefix = "[Stream Chat] ";

            _requestUriFactory = new RequestUriFactory(authProvider: this, connectionProvider: this, () =>
                BuildStreamClientHeader(new UnityApplicationInfo()));

            _httpClient.AddDefaultCustomHeader("stream-auth-type", DefaultStreamAuthType);
            var header = BuildStreamClientHeader(_applicationInfo);
            _httpClient.AddDefaultCustomHeader("X-Stream-Client", header);

            //StreamTodo: move to factory
            var coordinatorReconnect = new ReconnectScheduler(_timeService, this, _networkMonitor);
            var sfuReconnect = new ReconnectScheduler(_timeService, this, _networkMonitor);

            //StreamTodo: move to factory
            _coordinatorWS = new CoordinatorWebSocket(coordinatorWebSocket, coordinatorReconnect, _requestUriFactory, _serializer, _timeService, _logs);
            var sfuWebSocketWrapper = new SfuWebSocket(sfuWebSocket, sfuReconnect, _requestUriFactory, _serializer, _timeService, _logs, _applicationInfo, SDKVersion);
            
            _coordinatorWS.ConnectionStateChanged += OnCoordinatorConnectionStateChanged;

            // StreamTODO: setup a wrapper around WS that handles health checks, error handling and reconnections
            // _coordinatorWebSocketOld.ConnectionFailed += OnCoordinatorWebsocketsConnectionFailed;
            // _coordinatorWebSocketOld.Disconnected += OnCoordinatorWebsocketDisconnected;
            //
            // _sfuWebSocketOld.ConnectionFailed += OnSfuWebsocketsConnectionFailed;
            // _sfuWebSocketOld.Disconnected += OnSfuWebsocketDisconnected;

            InternalVideoClientApi
                = new InternalVideoClientApi(httpClient, serializer, logs, _requestUriFactory, lowLevelClient: this);
   
            _rtcSession = new RtcSession(sfuWebSocketWrapper, _logs);

            RegisterCoordinatorEventHandlers();

            LogErrorIfUpdateIsNotBeingCalled();
        }

        private void OnCoordinatorConnectionStateChanged(ConnectionState previous, ConnectionState current)
        {
            ConnectionStateChanged?.Invoke(previous, current);

            if (current == ConnectionState.Disconnected)
            {
                Disconnected?.Invoke();
            }
        }

        // [Obsolete("Delete this")]
        // public void ConnectUser(AuthCredentials userAuthCredentials)
        // {
        //     SetConnectionCredentials(userAuthCredentials);
        //     Connect();
        // }
        //
        // [Obsolete("Delete this")]
        // //StreamTodo: we should leave Async option only, this was left for backward compatibility
        // public void Connect()
        // {
        //     SetConnectionCredentials(_authCredentials);
        //
        //     if (!ConnectionState.IsValidToConnect())
        //     {
        //         throw new InvalidOperationException("Attempted to connect, but client is in state: " + ConnectionState);
        //     }
        //
        //     TryCancelWaitingForUserConnection();
        //
        //     //StreamTodo: hidden dependency on SetUser being called
        //     //StreamTodo: remove injected Func
        //     var coordinatorConnectUri =
        //         _requestUriFactory.CreateCoordinatorConnectionUri();
        //
        //     _logs.Info($"Attempt to connect to: {coordinatorConnectUri}");
        //
        //     ConnectionState = ConnectionState.Connecting;
        //
        //     //StreamTodo: this is async
        //     _coordinatorWS.ConnectAsync();
        //
        //     UpdateLocationHintAsync().LogIfFailed();
        // }

        public Task ConnectUserAsync(AuthCredentials authCredentials, CancellationToken cancellationToken = default) 
            => ConnectUserAsync(authCredentials.ApiKey, authCredentials.UserId, authCredentials.UserToken, cancellationToken);
        
        public async Task ConnectUserAsync(string apiKey, string userId, string userToken,
            CancellationToken cancellationToken = default)
        {
            //StreamTodo: validate input
            
            SetConnectionCredentials(new AuthCredentials(apiKey, userId, userToken));

            if (!ConnectionState.IsValidToConnect())
            {
                throw new InvalidOperationException("Attempted to connect, but client is in state: " + ConnectionState);
            }

            //TryCancelWaitingForUserConnection();

            var connectUri = _requestUriFactory.CreateCoordinatorConnectionUri();

            _logs.Info($"Connect to coordinator: {connectUri}");

            await _coordinatorWS.ConnectAsync(cancellationToken);
            await UpdateLocationHintAsync();
        }

        public async Task ConnectUserAsync(string apiKey, string userId, ITokenProvider tokenProvider)
        {
            //StreamTodo: validate input
            _tokenProvider = tokenProvider;
            
            SetPartialConnectionCredentials(apiKey, userId);

            await RefreshAuthTokenFromProviderAsync();
            await ConnectUserAsync(_authCredentials.ApiKey, _authCredentials.UserId, _authCredentials.UserToken);
        }

        //StreamTodo: make few attempts + can be awaited by the JoinCallAsync + support reconnections
        private async Task UpdateLocationHintAsync()
        {
            var headers = new List<KeyValuePair<string, IEnumerable<string>>>();
            await _httpClient.HeadAsync(LocationHintWebUri, headers);
            
            var locationHeader = headers.FirstOrDefault(_ => _.Key.ToLower() == LocationHintHeaderKey);
            if (locationHeader.Key.IsNullOrEmpty() || !locationHeader.Value.Any())
            {
                _logs.Error($"Failed to get `{LocationHintHeaderKey}` header from `{LocationHintWebUri}` request");
                return;
            }

            _locationHint = locationHeader.Value.First();
            _logs.Info("Location Hint: " + _locationHint);
        }

        public async Task DisconnectAsync()
        {
            await _coordinatorWS.DisconnectAsync(WebSocketCloseStatus.NormalClosure, "User called Disconnect");
            await _rtcSession.StopAsync();
        }

        public void Update()
        {
#if !STREAM_TESTS_ENABLED
            _updateCallReceived = true;
#endif

            _coordinatorWS.Update();
            _rtcSession.Update();
        }

        //StreamTodo: if ring and notify can't be both true then perhaps enum NotifyMode.Ring, NotifyMode.Notify?
        //StreamTodo: add CreateCallOptions
        public async Task JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring, bool notify)
        {
            // StreamTodo: check state if we don't have an active session already
            var locationHint = await GetLocationHintAsync();

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

            var joinCallResponse = await InternalVideoClientApi.JoinCallAsync(callType, callId, joinCallRequest);
            await _rtcSession.StartAsync(joinCallResponse);
        }

        // //StreamTodo: move this to injected config object
        // public void SetReconnectStrategySettings(ReconnectStrategy reconnectStrategy, float? exponentialMinInterval,
        //     float? exponentialMaxInterval, float? constantInterval)
        // {
        //     _reconnectScheduler.SetReconnectStrategySettings(reconnectStrategy, exponentialMinInterval,
        //         exponentialMaxInterval, constantInterval);
        // }

        public void Dispose()
        {

            // _reconnectScheduler.Dispose();

            //TryCancelWaitingForUserConnection();
            
            _coordinatorWS.ConnectionStateChanged -= OnCoordinatorConnectionStateChanged;
            _coordinatorWS.Dispose();

            // _coordinatorWebSocketOld.ConnectionFailed -= OnCoordinatorWebsocketsConnectionFailed;
            // _coordinatorWebSocketOld.Disconnected -= OnCoordinatorWebsocketDisconnected;
            // _coordinatorWebSocketOld.Dispose();
            //
            // _sfuWebSocketOld.ConnectionFailed -= OnSfuWebsocketsConnectionFailed;
            // _sfuWebSocketOld.Disconnected -= OnCoordinatorWebsocketDisconnected;
            // _sfuWebSocketOld.Dispose();

            _updateMonitorCts.Cancel();
            _rtcSession?.Dispose();
        }

        string IAuthProvider.ApiKey => _authCredentials.ApiKey;
        string IAuthProvider.UserToken => _authCredentials.UserToken;
        string IAuthProvider.UserId => _authCredentials.UserId;
        string IAuthProvider.StreamAuthType => DefaultStreamAuthType;
        string IConnectionProvider.ConnectionId => _connectionId;
        Uri IConnectionProvider.ServerUri => ServerBaseUrl;
        
        internal event Action<CallCreatedEvent> InternalCallCreatedEvent;
        internal event Action<CallUpdatedEvent> InternalCallUpdatedEvent;
        internal event Action<CallEndedEvent> InternalCallEndedEvent;
        internal event Action<ParticipantJoined> InternalParticipantJoinedEvent;
        internal event Action<ParticipantLeft> InternalParticipantLeftEvent;
        internal event Action<CallAcceptedEvent> InternalCallAcceptedEvent;
        internal event Action<CallRejectedEvent> InternalCallRejectedEvent;
        internal event Action<CallLiveStartedEvent> InternalCallLiveStartedEvent;
        internal event Action<CallMemberAddedEvent> InternalCallMemberAddedEvent;
        internal event Action<CallMemberRemovedEvent> InternalCallMemberRemovedEvent;
        internal event Action<CallMemberUpdatedEvent> InternalCallMemberUpdatedEvent;
        internal event Action<CallMemberUpdatedPermissionEvent> InternalCallMemberUpdatedPermissionEvent;
        internal event Action<CallNotificationEvent> InternalCallNotificationEvent;
        internal event Action<PermissionRequestEvent> InternalPermissionRequestEvent;
        internal event Action<UpdatedCallPermissionsEvent> InternalUpdatedCallPermissionsEvent;
        internal event Action<CallReactionEvent> InternalCallReactionEvent;
        internal event Action<CallRecordingStartedEvent> InternalCallRecordingStartedEvent;
        internal event Action<CallRecordingStoppedEvent> InternalCallRecordingStoppedEvent;
        internal event Action<BlockedUserEvent> InternalBlockedUserEvent;
        internal event Action<CallBroadcastingStartedEvent> InternalCallBroadcastingStartedEvent;
        internal event Action<CallBroadcastingStoppedEvent> InternalCallBroadcastingStoppedEvent;
        internal event Action<CallRingEvent> InternalCallRingEvent;
        internal event Action<CallSessionEndedEvent> InternalCallSessionEndedEvent;
        internal event Action<CallSessionStartedEvent> InternalCallSessionStartedEvent;
        internal event Action<BlockedUserEvent> InternalCallUnblockedUserEvent;
        internal event Action<ConnectionErrorEvent> InternalConnectionErrorEvent;
        internal event Action<CustomVideoEvent> InternalCustomVideoEvent;
        
        internal IInternalVideoClientApi InternalVideoClientApi { get; }
        
        //StreamTodo: 2 ConnectUserAsync overloads: with tokenProvider and with the token directly

        //StreamTodo: check if still needed or unify with other ConnectUserAsync
        // internal async Task<OwnUserResponse> ConnectUserAsync(string apiKey, string userId,
        //     ITokenProvider tokenProvider, CancellationToken cancellationToken = default)
        // {
        //     if (!ConnectionState.IsValidToConnect())
        //     {
        //         throw new InvalidOperationException("Attempted to connect, but client is in state: " + ConnectionState);
        //     }
        //
        //     _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        //     SetPartialConnectionCredentials(apiKey, userId);
        //
        //     TryCancelWaitingForUserConnection();
        //
        //     _connectUserCancellationToken = cancellationToken;
        //
        //     _connectUserCancellationTokenSource
        //         = CancellationTokenSource.CreateLinkedTokenSource(_connectUserCancellationToken);
        //     _connectUserCancellationTokenSource.Token.Register(TryCancelWaitingForUserConnection);
        //
        //     _connectUserTaskSource = new TaskCompletionSource<OwnUserResponse>();
        //
        //     try
        //     {
        //         await RefreshAuthTokenFromProviderAsync();
        //
        //         await _coordinatorWS.ConnectAsync();
        //
        //         // StreamTODO: Do we receive a user here?
        //         var ownUserDto = await _connectUserTaskSource.Task;
        //         return ownUserDto;
        //     }
        //     catch (Exception e)
        //     {
        //         _logs.Exception(e);
        //         throw;
        //     }
        // }

        private const string DefaultStreamAuthType = "jwt";

        private readonly IPersistentWebSocket _coordinatorWS;
        
        private readonly ISerializer _serializer;
        private readonly ILogs _logs;
        private readonly ITimeService _timeService;
        private readonly INetworkMonitor _networkMonitor;
        private readonly IRequestUriFactory _requestUriFactory;
        private readonly IHttpClient _httpClient;
        private readonly IStreamClientConfig _config;

        // private readonly object _websocketConnectionFailedFlagLock = new object();
        // private readonly object _sfuWebsocketConnectionFailedFlagLock = new object();

        //private TaskCompletionSource<OwnUserResponse> _connectUserTaskSource;
        private CancellationToken _connectUserCancellationToken;
        private CancellationTokenSource _connectUserCancellationTokenSource;
        private CancellationTokenSource _updateMonitorCts;

        private AuthCredentials _authCredentials;

        private string _connectionId;

        private bool _updateCallReceived;

        // private bool _websocketConnectionFailed;
        // private bool _sfuWebsocketConnectionFailed;
        private ITokenProvider _tokenProvider;

        private string _locationHint;
        private readonly IApplicationInfo _applicationInfo;
        private readonly RtcSession _rtcSession;

        private async Task RefreshAuthTokenFromProviderAsync()
        {
#if STREAM_DEBUG_ENABLED
            _logs.Info($"Request new auth token for user `{_authCredentials.UserId}`");
#endif
            try
            {
                var token = await _tokenProvider.GetTokenAsync(_authCredentials.UserId);
                _authCredentials = _authCredentials.CreateWithNewUserToken(token);
                SetConnectionCredentials(_authCredentials);

#if STREAM_DEBUG_ENABLED
                _logs.Info($"auth token received for user `{_authCredentials.UserId}`: " + token);
#endif
            }
            catch (Exception e)
            {
                throw new TokenProviderException(
                    $"Failed to get token from the {nameof(ITokenProvider)}. Inspect {nameof(e.InnerException)} for more information. ",
                    e);
            }
        }
        
        private async Task<string> GetLocationHintAsync()
        {
            // StreamTodo: attempt to get location hint if not fetched already + perhaps there's an ongoing request and we can just wait
            if (_locationHint.IsNullOrEmpty())
            {
                _logs.Error("No location hint");
                throw new InvalidOperationException("No location hint");
            }

            return _locationHint;
        }

//         private void TryCancelWaitingForUserConnection()
//         {
//             if (_connectUserTaskSource == null)
//             {
//                 return;
//             }
//
//             var isConnectTaskRunning = _connectUserTaskSource.Task != null && !_connectUserTaskSource.Task.IsCompleted;
//             var isCancellationRequested = _connectUserCancellationTokenSource.IsCancellationRequested;
//
//             if (isConnectTaskRunning && !isCancellationRequested)
//             {
// #if STREAM_DEBUG_ENABLED
//                 _logs.Info($"Try Cancel {_connectUserTaskSource}");
// #endif
//                 _connectUserTaskSource.TrySetCanceled();
//             }
//         }

//         // StreamTodo: refactor how connected state is determined
//         private void OnCoordinatorWebsocketDisconnected()
//         {
// #if STREAM_DEBUG_ENABLED
//             _logs.Warning("Coordinator Websocket Disconnected");
// #endif
//             //ConnectionState = ConnectionState.Disconnected;
//         }
//
//         private void OnSfuWebsocketDisconnected()
//         {
// #if STREAM_DEBUG_ENABLED
//             _logs.Warning("Coordinator Websocket Disconnected");
// #endif
//             //ConnectionState = ConnectionState.Disconnected;
//         }

        // /// <summary>
        // /// This event can be called by a background thread and we must propagate it on the main thread
        // /// Otherwise any call to Unity API would result in Exception. Unity API can only be called from the main thread
        // /// </summary>
        // private void OnCoordinatorWebsocketsConnectionFailed()
        // {
        //     lock (_websocketConnectionFailedFlagLock)
        //     {
        //         _websocketConnectionFailed = true;
        //     }
        // }

        // /// <summary>
        // /// This event can be called by a background thread and we must propagate it on the main thread
        // /// Otherwise any call to Unity API would result in Exception. Unity API can only be called from the main thread
        // /// </summary>
        // private void OnSfuWebsocketsConnectionFailed()
        // {
        //     lock (_sfuWebsocketConnectionFailedFlagLock)
        //     {
        //         _sfuWebsocketConnectionFailed = true;
        //     }
        // }

//         private void TryHandleWebsocketsConnectionFailed()
//         {
//             lock (_websocketConnectionFailedFlagLock)
//             {
//                 if (!_websocketConnectionFailed)
//                 {
//                     return;
//                 }
//
//                 _websocketConnectionFailed = false;
//             }
//
// #if STREAM_DEBUG_ENABLED
//             _logs.Warning("Websocket connection failed");
// #endif
//
//             ConnectionState = ConnectionState.Disconnected;
//         }

//         /// <summary>
//         /// Based on receiving initial health check event from the server
//         /// </summary>
//         private void OnConnectionConfirmed(ConnectedEvent connectedEvent)
//         {
//             //StreamTodo: resolve issue that expired token also triggers connection confirmed that gets immediately disconnected
//
//             _connectionId = connectedEvent.ConnectionId;
// #pragma warning disable 0618
//             //LocalUser = connectedEvent.Me;
// #pragma warning restore 0618
//
//             ConnectionState = ConnectionState.Connected;
//
//             _connectUserTaskSource?.SetResult(connectedEvent.Me);
//
//             _logs.Info("Connection confirmed by server with connection id: " + _connectionId);
//             //Connected?.Invoke(connectedEvent.Me);
//             //InternalConnected?.Invoke(eventHealthCheckInternalDto);
//         }

        // private void TryToReconnect()
        // {
        //     if (!ConnectionState.IsValidToConnect() || !NextReconnectTime.HasValue)
        //     {
        //         return;
        //     }
        //
        //     if (NextReconnectTime.Value > _timeService.Time)
        //     {
        //         return;
        //     }
        //
        //     Reconnecting?.Invoke();
        //
        //     if (_tokenProvider != null)
        //     {
        //         ConnectUserAsync(_authCredentials.ApiKey, _authCredentials.UserId, _tokenProvider).LogIfFailed();
        //     }
        //     else
        //     {
        //         ConnectUserAsync(_authCredentials).LogIfFailed();
        //         //Connect();
        //     }
        // }

        private void RegisterCoordinatorEventHandlers()
        {
            _coordinatorWS.RegisterEventType<CallCreatedEvent>(CoordinatorEventType.CallCreated,
                e => InternalCallCreatedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallUpdatedEvent>(CoordinatorEventType.CallUpdated,
                e => InternalCallUpdatedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallEndedEvent>(CoordinatorEventType.CallEnded,
                e => InternalCallEndedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<ParticipantJoined>(CoordinatorEventType.CallSessionParticipantJoined,
                e => InternalParticipantJoinedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<ParticipantLeft>(CoordinatorEventType.CallSessionParticipantLeft,
                e => InternalParticipantLeftEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallAcceptedEvent>(CoordinatorEventType.CallAccepted,
                e => InternalCallAcceptedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallRejectedEvent>(CoordinatorEventType.CallRejected,
                e => InternalCallRejectedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallLiveStartedEvent>(CoordinatorEventType.CallLiveStarted,
                e => InternalCallLiveStartedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallMemberAddedEvent>(CoordinatorEventType.CallMemberAdded,
                e => InternalCallMemberAddedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallMemberRemovedEvent>(CoordinatorEventType.CallMemberRemoved,
                e => InternalCallMemberRemovedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallMemberUpdatedEvent>(CoordinatorEventType.CallMemberUpdated,
                e => InternalCallMemberUpdatedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallMemberUpdatedPermissionEvent>(CoordinatorEventType.CallMemberUpdatedPermission,
                e => InternalCallMemberUpdatedPermissionEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallNotificationEvent>(CoordinatorEventType.CallNotification,
                e => InternalCallNotificationEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<PermissionRequestEvent>(CoordinatorEventType.CallPermissionRequest,
                e => InternalPermissionRequestEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<UpdatedCallPermissionsEvent>(CoordinatorEventType.CallPermissionsUpdated,
                e => InternalUpdatedCallPermissionsEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallReactionEvent>(CoordinatorEventType.CallReactionNew,
                e => InternalCallReactionEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallRecordingStartedEvent>(CoordinatorEventType.CallRecordingStarted,
                e => InternalCallRecordingStartedEvent?.Invoke(e));
            _coordinatorWS.RegisterEventType<CallRecordingStoppedEvent>(CoordinatorEventType.CallRecordingStopped,
                e => InternalCallRecordingStoppedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<BlockedUserEvent>(CoordinatorEventType.CallBlockedUser,
                e => InternalBlockedUserEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallBroadcastingStartedEvent>(CoordinatorEventType.CallBroadcastingStarted,
                e => InternalCallBroadcastingStartedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallBroadcastingStoppedEvent>(CoordinatorEventType.CallBroadcastingStopped,
                e => InternalCallBroadcastingStoppedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallRingEvent>(CoordinatorEventType.CallRing,
                e => InternalCallRingEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallSessionEndedEvent>(CoordinatorEventType.CallSessionEnded,
                e => InternalCallSessionEndedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CallSessionStartedEvent>(CoordinatorEventType.CallSessionStarted,
                e => InternalCallSessionStartedEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<BlockedUserEvent>(CoordinatorEventType.CallUnblockedUser,
                e => InternalCallUnblockedUserEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<ConnectionErrorEvent>(CoordinatorEventType.ConnectionError,
                e => InternalConnectionErrorEvent?.Invoke(e));

            _coordinatorWS.RegisterEventType<CustomVideoEvent>(CoordinatorEventType.Custom,
                e => InternalCustomVideoEvent?.Invoke(e));

        }

//         private void UpdateHealthCheck()
//         {
//             if (ConnectionState != ConnectionState.Connected)
//             {
//                 return;
//             }
//
//             var timeSinceLastHealthCheckSent = _timeService.Time - _lastHealthCheckSendTime;
//             if (timeSinceLastHealthCheckSent > HealthCheckSendInterval)
//             {
//                 PingHealthCheck();
//             }
//
//             var timeSinceLastHealthCheck = _timeService.Time - _lastHealthCheckReceivedTime;
//             if (timeSinceLastHealthCheck > HealthCheckMaxWaitingTime)
//             {
//                 _logs.Warning($"Health check was not received since: {timeSinceLastHealthCheck}, reset connection");
//                 _coordinatorWebSocketOld
//                     .DisconnectAsync(WebSocketCloseStatus.InternalServerError,
//                         $"Health check was not received since: {timeSinceLastHealthCheck}")
//                     .ContinueWith(_ => _logs.Exception(_.Exception), TaskContinuationOptions.OnlyOnFaulted);
//             }
//         }
//
//         private void PingHealthCheck()
//         {
//             var healthCheck = new HealthCheckEvent();
//
//             _coordinatorWebSocketOld.Send(_serializer.Serialize(healthCheck));
//             _lastHealthCheckSendTime = _timeService.Time;
//
// #if STREAM_DEBUG_ENABLED
//             _logs.Info("Health check sent");
// #endif
//         }

        // private void HandleHealthCheckEvent(HealthCheckEvent healthCheckEvent)
        // {
        //     _logs.Info("Health check received");
        //     _lastHealthCheckReceivedTime = _timeService.Time;
        // }
        //
        // private void HandleConnectionOkEvent(ConnectedEvent connectedEvent)
        // {
        //     _connectionId = connectedEvent.ConnectionId;
        //
        //     ConnectionState = ConnectionState.Connected;
        //
        //     _connectUserTaskSource?.SetResult(connectedEvent.Me);
        //
        //     _logs.Info("Connection confirmed by server with connection id: " + _connectionId);
        //     Connected?.Invoke();
        // }

        private static bool IsUserIdValid(string userId)
        {
            var r = new Regex("^[a-zA-Z0-9@_-]+$");
            return r.IsMatch(userId);
        }

        private static string Base64UrlEncode(byte[] input)
            => Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .Trim('=');

        private void SetConnectionCredentials(AuthCredentials credentials)
        {
            if (credentials.IsAnyEmpty())
            {
                throw new StreamMissingAuthCredentialsException(
                    "Please provide valid credentials: `Api Key`, 'User id`, `User token`");
            }

            _authCredentials = credentials;
            _httpClient.SetDefaultAuthenticationHeader(credentials.UserToken);
        }

        //StreamTodo: make it more clear that we either receive full set of credentials or apiKey, userId and the token provider
        private void SetPartialConnectionCredentials(string apiKey, string userId)
        {
            _authCredentials = new AuthCredentials(apiKey, userId, string.Empty);
        }

        private void LogErrorIfUpdateIsNotBeingCalled()
        {
            _updateMonitorCts = new CancellationTokenSource();

            //StreamTodo: temporarily disable update monitor when tests are enabled -> investigate why some tests trigger this error
#if !STREAM_TESTS_ENABLED
            const int timeout = 2;
            Task.Delay(timeout * 1000, _updateMonitorCts.Token).ContinueWith(t =>
            {
                if (!_updateCallReceived && !_updateMonitorCts.IsCancellationRequested &&
                    ConnectionState != ConnectionState.Closing)
                {
                    _logs.Error(
                        $"Connection is not being updated. Please call the `{nameof(StreamVideoLowLevelClient)}.{nameof(Update)}` method per frame. Connection state: {ConnectionState}");
                }
            }, _updateMonitorCts.Token);
#endif
        }

        private static string BuildStreamClientHeader(IApplicationInfo applicationInfo)
        {
            var sb = new StringBuilder();
            sb.Append($"stream-video-unity-client-");
            sb.Append(SDKVersion);
            // sb.Append("|");
            //
            // sb.Append("os=");
            // sb.Append(applicationInfo.OperatingSystem);
            // sb.Append("|");
            //
            // sb.Append("platform=");
            // sb.Append(applicationInfo.Platform);
            // sb.Append("|");
            //
            // sb.Append("engine=");
            // sb.Append(applicationInfo.Engine);
            // sb.Append("|");
            //
            // sb.Append("engine_version=");
            // sb.Append(applicationInfo.EngineVersion);
            // sb.Append("|");
            //
            // sb.Append("screen_size=");
            // sb.Append(applicationInfo.ScreenSize);
            // sb.Append("|");
            //
            // sb.Append("memory_size=");
            // sb.Append(applicationInfo.MemorySize);
            // sb.Append("|");
            //
            // sb.Append("graphics_memory_size=");
            // sb.Append(applicationInfo.GraphicsMemorySize);

            return sb.ToString();
        }


    }
}