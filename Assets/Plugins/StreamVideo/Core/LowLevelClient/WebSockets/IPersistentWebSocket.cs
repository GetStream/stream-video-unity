using System;
using System.Threading.Tasks;
using StreamVideo.Libs.Auth;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal interface IPersistentWebSocket : IDisposable
    {
        void Update();

        void RegisterEventType<TDto, TEvent>(string key,
            Action<TEvent, TDto> handler, Action<TDto> internalHandler = null)
            where TEvent : ILoadableFrom<TDto, TEvent>, new();

        void RegisterEventType<TDto>(string key,
            Action<TDto> internalHandler = null);

        ITokenProvider AuthTokenProvider { get; set; }
        AuthCredentials AuthCredentials { get; set; }
        Task ConnectAsync();
    }
}