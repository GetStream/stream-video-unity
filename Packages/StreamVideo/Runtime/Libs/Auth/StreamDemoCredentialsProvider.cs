using System;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Libs.Auth
{
    /// <summary>
    /// Fetches demo/test credentials (API key + user token) from the Pronto endpoint.
    /// DO NOT USE IN PRODUCTION. These credentials are rate-limited and meant only for internal testing and demo apps.
    /// Customer accounts have a FREE tier for testing — please register at https://getstream.io/ to get your own app ID and credentials.
    /// </summary>
    public class StreamDemoCredentialsProvider
    {
        public StreamDemoCredentialsProvider(IHttpClient httpClient, ISerializer serializer)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Fetch demo credentials from <c>https://pronto.getstream.io/api/auth/create-token</c>.
        /// The returned <see cref="AuthCredentials"/> contains the API key, user ID, and signed token.
        /// DO NOT USE IN PRODUCTION. Customer accounts have a FREE tier for testing —
        /// please register at https://getstream.io/ to get your own app ID and credentials.
        /// </summary>
        public async Task<AuthCredentials> GetDemoCredentialsAsync(
            string userId,
            StreamEnvironment environment = StreamEnvironment.Demo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID must not be null or empty.", nameof(userId));
            }

            var uri = new UriBuilder
            {
                Scheme = "https",
                Host = Host,
                Path = Path,
                Query = $"user_id={Uri.EscapeDataString(userId)}&environment={ToQueryValue(environment)}",
            }.Uri;

            var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = null;
                try
                {
                    var error = _serializer.Deserialize<ErrorResponse>(response.Result);
                    errorMessage = error?.Error;
                }
                catch
                {
                    // ignored — use raw response below
                }

                throw new Exception(
                    $"Failed to get demo credentials. Status code: {response.StatusCode}, " +
                    $"Error: {errorMessage ?? response.Result}");
            }

            var result = _serializer.Deserialize<CredentialsResponse>(response.Result);
            return new AuthCredentials(result.ApiKey, result.UserId, result.Token);
        }

        private const string Host = "pronto.getstream.io";
        private const string Path = "/api/auth/create-token";

        private static string ToQueryValue(StreamEnvironment environment)
        {
            switch (environment)
            {
                case StreamEnvironment.Demo: return "demo";
                case StreamEnvironment.Pronto: return "pronto";
                default: throw new ArgumentOutOfRangeException(nameof(environment), environment,
                    $"Unsupported environment: {environment}");
            }
        }

        private class CredentialsResponse
        {
            public string UserId;
            public string ApiKey;
            public string Token;
        }

        private class ErrorResponse
        {
            public string Error;
        }

        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
    }
}
