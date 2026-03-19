using System;
using StreamVideo.Core.Auth;
using StreamVideo.Core.Web;
using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    /// <summary>
    /// Factory for creating <see cref="SfuWebSocket"/> instances.
    /// </summary>
    internal class SfuWebSocketFactory : ISfuWebSocketFactory
    {
        public SfuWebSocketFactory(Func<IWebsocketClient> websocketClientFactory, IAuthProvider authProvider,
            IRequestUriFactory requestUriFactory, ISerializer serializer, ITimeService timeService,
            INetworkMonitor networkMonitor, ILogs logs, IApplicationInfo applicationInfo, Version sdkVersion)
        {
            _websocketClientFactory = websocketClientFactory ??
                                      throw new ArgumentNullException(nameof(websocketClientFactory));
            _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
            _requestUriFactory = requestUriFactory ?? throw new ArgumentNullException(nameof(requestUriFactory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _applicationInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo));
            _sdkVersion = sdkVersion ?? throw new ArgumentNullException(nameof(sdkVersion));
        }

        /// <inheritdoc/>
        public ISfuWebSocket Create()
        {
            var websocketClient = _websocketClientFactory();

            // SFU reconnection is handled by RtcSession, not by the ReconnectScheduler
            var reconnectScheduler = new ReconnectScheduler(_timeService, _networkMonitor,
                shouldReconnect: () => false);

            return new SfuWebSocket(websocketClient, reconnectScheduler, _authProvider, _requestUriFactory, _serializer,
                _timeService, _logs, _applicationInfo, _sdkVersion);
        }

        private readonly Func<IWebsocketClient> _websocketClientFactory;
        private readonly IAuthProvider _authProvider;
        private readonly IRequestUriFactory _requestUriFactory;
        private readonly ISerializer _serializer;
        private readonly ITimeService _timeService;
        private readonly INetworkMonitor _networkMonitor;
        private readonly ILogs _logs;
        private readonly IApplicationInfo _applicationInfo;
        private readonly Version _sdkVersion;
    }
}