using System;
using System.Collections.Generic;
using StreamVideo.Libs.Utils;
using StreamVideo.Core.Auth;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Web
{
    /// <summary>
    /// Requests Uri Factory
    /// </summary>
    internal class RequestUriFactory : IRequestUriFactory
    {
        public RequestUriFactory(IAuthProvider authProvider, IStreamVideoLowLevelClient connectionProvider,
            Func<string> clientInfoFactory)
        {
            _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _clientInfoFactory = clientInfoFactory ?? throw new ArgumentNullException(nameof(clientInfoFactory));
        }

        public Uri CreateCoordinatorConnectionUri()
        {
            var uriParams = new Dictionary<string, string>
            {
                { "api_key", _authProvider.ApiKey },
                { "stream-auth-type", _authProvider.StreamAuthType },
                { "X-Stream-Client", _clientInfoFactory() }
            };

            var uriBuilder = new UriBuilder(_connectionProvider.ServerUri)
                { Path = "video/connect", Query = uriParams.ToQueryParameters() };

            return uriBuilder.Uri;
        }

        public Uri CreateSfuConnectionUri(string sfuUrl)
        {
#if STREAM_LOCAL_SFU
            return StreamVideoLowLevelClient.LocalSfuWebSocketUri;
#endif
            sfuUrl = sfuUrl.Replace("/twirp", "/ws");
            return new UriBuilder(sfuUrl) { Scheme = "wss" }.Uri;
        }

        public Uri CreateEndpointUri(string endpoint, Dictionary<string, string> parameters = null)
        {
            var requestParameters = GetDefaultParameters();

            if (parameters != null)
            {
                foreach (var p in parameters.Keys)
                {
                    requestParameters[p] = parameters[p];
                }
            }

            return CreateRequestUri(endpoint, requestParameters);
        }

        private readonly IAuthProvider _authProvider;
        private readonly IStreamVideoLowLevelClient _connectionProvider;
        private readonly Func<string> _clientInfoFactory;

        private Dictionary<string, string> GetDefaultParameters()
            => new Dictionary<string, string>
            {
                { "user_id", _authProvider.UserId },
                { "api_key", _authProvider.ApiKey },
                { "connection_id", _connectionProvider.ConnectionId },
            };

        private Uri CreateRequestUri(string endPoint, IReadOnlyDictionary<string, string> parameters)
            => CreateRequestUri(endPoint, parameters.ToQueryParameters());

        private Uri CreateRequestUri(string endPoint, string query)
        {
            if (!endPoint.StartsWith('/'))
            {
                //StreamTodo: error if debug mode
                endPoint = "/" + endPoint;
            }

            var uriBuilder = new UriBuilder(_connectionProvider.ServerUri)
                { Path = $"video{endPoint}", Scheme = "https", Query = query };

            return uriBuilder.Uri;
        }
    }
}