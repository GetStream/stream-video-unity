using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StreamVideo.Core.LowLevelClient
{
    internal interface ISfuClient
    {
        SessionID SessionId { get; }

        Task<TResponse> RpcCallAsync<TRequest, TResponse>(TRequest request,
            Func<HttpClient, TRequest, CancellationToken, Task<TResponse>> rpcCallAsync, string debugRequestName,
            CancellationToken cancellationToken, Func<TResponse, StreamVideo.v1.Sfu.Models.Error> getError,
            bool preLog = false, bool postLog = true);
    }
}