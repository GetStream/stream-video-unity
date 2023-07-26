using System;
using System.Collections.Generic;
using StreamVideo.Libs.Serialization;
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
            ISerializer serializer)
        {
            _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        //StreamTodo: remove clientInfoFactory
        public Uri CreateCoordinatorConnectionUri(Func<string> clientInfoFactory)
        {
            var uriParams = new Dictionary<string, string>
            {
                { "api_key", _authProvider.ApiKey },
                { "stream-auth-type", _authProvider.StreamAuthType },
                { "X-Stream-Client", clientInfoFactory() }
            };

            var uriBuilder = new UriBuilder(_connectionProvider.ServerUri)
                { Path = "video/connect", Query = uriParams.ToQueryParameters() };

            return uriBuilder.Uri;
        }

        public Uri CreateSfuConnectionUri(string sfuUrl)
        {
            var absolutePath = sfuUrl;
            absolutePath = absolutePath.Replace("/twirp", "/ws");

            return new UriBuilder(absolutePath) { Scheme = "wss" }.Uri;
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
        private readonly ISerializer _serializer;
        private readonly IStreamVideoLowLevelClient _connectionProvider;

        private Dictionary<string, string> GetDefaultParameters() =>
            new Dictionary<string, string>
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