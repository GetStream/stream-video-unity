using StreamChat.Core.LowLevelClient.API.Internal;
using StreamVideo.Core.Web;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.LowLevelClient.API.Internal
{
    internal class InternalVideoClientApi : InternalApiClientBase, IInternalVideoClientApi
    {
        public InternalVideoClientApi(IHttpClient httpClient, ISerializer serializer, ILogs logs,
            IRequestUriFactory requestUriFactory, IStreamVideoLowLevelClient lowLevelClient) : base(httpClient,
            serializer, logs, requestUriFactory, lowLevelClient)
        {
        }
    }
}