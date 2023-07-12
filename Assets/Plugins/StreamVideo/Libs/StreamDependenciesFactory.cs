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
    public static class StreamDependenciesFactory
    {
        public static ILogs CreateLogger(LogLevel logLevel = LogLevel.All)
            => new UnityLogs(logLevel);

        public static IWebsocketClient CreateWebsocketClient(ILogs logs, bool isDebugMode = false)
        {

#if UNITY_WEBGL
            //StreamTodo: handle debug mode
            return new NativeWebSocketWrapper(logs, isDebugMode: isDebugMode);
#else
            return new WebsocketClient(logs, isDebugMode: isDebugMode);
#endif
        }

        public static IHttpClient CreateHttpClient()
        {
#if UNITY_WEBGL
            return new UnityWebRequestHttpClient();
#else
            return new HttpClientAdapter();
#endif
        }

        public static ISerializer CreateSerializer() => new NewtonsoftJsonSerializer();

        public static ITimeService CreateTimeService() => new UnityTime();

        public static IApplicationInfo CreateApplicationInfo() => new UnityApplicationInfo();
        
        public static ITokenProvider CreateTokenProvider(TokenProvider.TokenUriHandler urlFactory) => new TokenProvider(CreateHttpClient(), urlFactory);

        public static IStreamVideoClientRunner CreateChatClientRunner()
        {
            var go = new GameObject
            {
                name = "Stream Chat Client Runner",
#if !STREAM_DEBUG_ENABLED
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideAndDontSave
#endif
            };
            return go.AddComponent<StreamMonoBehaviourWrapper.UnityStreamVideoClientRunner>();
        }

        public static INetworkMonitor CreateNetworkMonitor() => new UnityNetworkMonitor();
    }
}