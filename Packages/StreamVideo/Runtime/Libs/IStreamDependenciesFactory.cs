using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.NetworkMonitors;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.VideoClientInstanceRunner;
using StreamVideo.Libs.Websockets;

namespace StreamVideo.Libs
{
    public interface IStreamDependenciesFactory
    {
        ILogs CreateLogger(LogLevel logLevel = LogLevel.All);

        IWebsocketClient CreateWebsocketClient(ILogs logs, bool isDebugMode = false);

        IHttpClient CreateHttpClient();

        ISerializer CreateSerializer();

        ITimeService CreateTimeService();

        IApplicationInfo CreateApplicationInfo();

        ITokenProvider CreateTokenProvider(TokenProvider.TokenUriHandler urlFactory);

        IStreamVideoClientRunner CreateChatClientRunner();

        INetworkMonitor CreateNetworkMonitor();
    }
}