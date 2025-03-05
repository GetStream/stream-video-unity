using Libs.Websockets;
using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.VideoClientInstanceRunner;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;
using UnityEngine;

namespace StreamVideo.Libs
{
    /// <summary>
    /// Factory that provides external dependencies for the Stream Chat Client.
    /// Stream chat client depends only on the interfaces therefore you can provide your own implementation for any of the dependencies
    /// </summary>
    public class StreamDependenciesFactory : IStreamDependenciesFactory
    {
        public virtual ILogs CreateLogger(LogLevel logLevel = LogLevel.All)
            => new UnityLogs(logLevel);

        public virtual IWebsocketClient CreateWebsocketClient(ILogs logs, bool isDebugMode = false)
        {

#if UNITY_WEBGL
            //StreamTodo: handle debug mode
            return new NativeWebSocketWrapper(logs, isDebugMode: isDebugMode);
#else
            return new WebsocketSharpClient(logs);
#endif
        }

        public virtual IHttpClient CreateHttpClient()
        {
#if UNITY_WEBGL
            return new UnityWebRequestHttpClient();
#else
            return new HttpClientAdapter();
#endif
        }

        public virtual ISerializer CreateSerializer() => new NewtonsoftJsonSerializer();

        public virtual ITimeService CreateTimeService() => new UnityTime();

        public virtual IApplicationInfo CreateApplicationInfo() => new UnityApplicationInfo();
        
        public virtual ITokenProvider CreateTokenProvider(TokenProvider.TokenUriHandler urlFactory) => new TokenProvider(CreateHttpClient(), urlFactory);

        public virtual IStreamVideoClientRunner CreateClientRunner()
        {
            var go = new GameObject
            {
                name = "Stream Client Runner",
#if !STREAM_DEBUG_ENABLED
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideAndDontSave
#endif
            };
            return go.AddComponent<StreamMonoBehaviourWrapper.UnityStreamVideoClientRunner>();
        }

        public virtual INetworkMonitor CreateNetworkMonitor() => new UnityNetworkMonitor();
    }
}