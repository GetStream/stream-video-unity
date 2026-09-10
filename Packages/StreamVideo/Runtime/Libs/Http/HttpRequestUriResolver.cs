using System;

namespace StreamVideo.Libs.Http
{
    /// <summary>
    /// Combines <see cref="System.Net.Http.HttpClient.BaseAddress"/> with Twirp paths such as
    /// <c>/twirp/stream.video.sfu.signal.SignalServer/SetPublisher</c>.
    /// Mono/IL2CPP treats a leading slash as <c>file:///twirp/...</c> and marks it absolute, so
    /// <see cref="Uri.IsAbsoluteUri"/> is not enough. The WebGL player then XHRs that file URI
    /// and the browser rejects it with "Not allowed to load local resource".
    /// </summary>
    internal static class HttpRequestUriResolver
    {
        public static Uri Resolve(Uri baseAddress, Uri requestUri)
        {
            if (requestUri == null)
            {
                if (baseAddress == null)
                {
                    throw new InvalidOperationException(
                        "HttpClient.BaseAddress must be set for relative SFU URLs.");
                }

                return baseAddress;
            }

            if (!NeedsBaseAddress(requestUri))
            {
                return requestUri;
            }

            if (baseAddress == null)
            {
                throw new InvalidOperationException(
                    "HttpClient.BaseAddress must be set for relative SFU URLs.");
            }

            return Combine(baseAddress, requestUri);
        }

        static bool NeedsBaseAddress(Uri requestUri)
        {
            if (!requestUri.IsAbsoluteUri)
            {
                return true;
            }

            return !IsHttpOrHttps(requestUri);
        }

        static Uri Combine(Uri baseAddress, Uri requestUri)
        {
            if (!requestUri.IsAbsoluteUri)
            {
                return new Uri(baseAddress, requestUri);
            }

            // file:///twirp/foo -> /twirp/foo. Do not pass the file Uri to new Uri(base, uri):
            // an absolute second argument replaces the base instead of combining.
            var relative = requestUri.AbsolutePath;
            if (!string.IsNullOrEmpty(requestUri.Query))
            {
                relative += requestUri.Query;
            }

            return new Uri(baseAddress, relative);
        }

        static bool IsHttpOrHttps(Uri requestUri)
        {
            return string.Equals(requestUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(requestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }
    }
}
