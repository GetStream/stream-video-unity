#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StreamVideo.Libs.Http
{
    /// <summary>
    /// <see cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)"/> resumes on the
    /// thread pool after a yield. WebGL has no thread pool, so SFU Twirp calls such as SetPublisher
    /// hang after create-call. This type sends through <see cref="UnityWebRequestHttpMessageHandler"/>
    /// and stays on the Unity main thread.
    /// </summary>
    internal sealed class WebGLHttpClient : HttpClient
    {
        readonly UnityWebRequestHttpMessageHandler _handler;

        public WebGLHttpClient()
            : this(new UnityWebRequestHttpMessageHandler())
        {
        }

        WebGLHttpClient(UnityWebRequestHttpMessageHandler handler)
            : base(handler)
        {
            _handler = handler;
            DefaultRequestHeaders.ExpectContinue = false;
        }

        public override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Mono/IL2CPP parses "/twirp/..." as file:///twirp/... (IsAbsoluteUri = true).
            request.RequestUri = HttpRequestUriResolver.Resolve(BaseAddress, request.RequestUri);

            foreach (var header in DefaultRequestHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return _handler.Send(request, cancellationToken);
        }
    }
}
#endif
