using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StreamVideo.Core.LowLevelClient
{
    //StreamTODO: rename to ISfuRpcClient?
    internal interface ISfuClient
    {
        SessionID SessionId { get; }
        CallingState CallState { get; }
        
        /// <summary>
        /// Returns the current session version. Used to ignore errors for outdated SFU WS
        /// </summary>
        int SessionVersion { get; }

        /// <summary>
        /// SFU hostname extracted from the full URL (scheme and path stripped).
        /// </summary>
        string SfuHost { get; }

        Task<TResponse> RpcCallAsync<TRequest, TResponse>(TRequest request,
            Func<HttpClient, TRequest, CancellationToken, Task<TResponse>> rpcCallAsync, string debugRequestName,
            CancellationToken cancellationToken, Func<TResponse, StreamVideo.v1.Sfu.Models.Error> getError,
            bool preLog = false, bool postLog = true);
    }
}