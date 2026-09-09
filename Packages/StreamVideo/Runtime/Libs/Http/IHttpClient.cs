using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StreamVideo.Libs.Http
{
    /// <summary>
    /// Http client
    /// </summary>
    public interface IHttpClient
    {
        void SetDefaultAuthenticationHeader(string value);

        void AddDefaultCustomHeader(string key, string value);

        Task<HttpResponse> GetAsync(Uri uri, CancellationToken cancellationToken = default);

        Task<HttpResponse> PostAsync(Uri uri, object content, CancellationToken cancellationToken = default);

        Task<HttpResponse> PutAsync(Uri uri, object content, CancellationToken cancellationToken = default);

        Task<HttpResponse> PatchAsync(Uri uri, object content, CancellationToken cancellationToken = default);

        Task<HttpResponse> DeleteAsync(Uri uri, CancellationToken cancellationToken = default);

        Task<HttpResponse> SendHttpRequestAsync(HttpMethodType methodType, Uri uri, object optionalRequestContent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a HEAD request without default authentication or custom headers.
        /// Used for unauthenticated discovery (location hint). Custom headers would
        /// trigger a CORS preflight that CloudFront/S3 does not allow.
        /// </summary>
        Task<HttpResponse> HeadAsync(Uri uri,
            ICollection<KeyValuePair<string, IEnumerable<string>>> resultHeaders = null, CancellationToken cancellationToken = default);
    }
}