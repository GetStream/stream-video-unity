#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace StreamVideo.Libs.Http
{
    /// <summary>
    /// WebGL has no working sockets-based <see cref="HttpClientHandler"/>. SFU Twirp RPCs still use
    /// <see cref="HttpClient"/>, so this handler sends those requests through UnityWebRequest.
    /// </summary>
    public sealed class UnityWebRequestHttpMessageHandler : HttpMessageHandler
    {
        // Match UnityWebRequestHttpClient: CORS preflight + POST share one timer.
        const int RequestTimeoutSeconds = 60;

        internal Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
            => SendAsync(request, cancellationToken);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.RequestUri == null || !request.RequestUri.IsAbsoluteUri || request.RequestUri.IsFile)
            {
                throw new InvalidOperationException(
                    "SFU UnityWebRequest requires an http(s) URL. Relative and file URIs become file:///twirp/... in the WebGL player.");
            }

            var unityRequest = new UnityWebRequest(request.RequestUri.AbsoluteUri, request.Method.Method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = RequestTimeoutSeconds
            };

            if (request.Content != null)
            {
                var body = await HttpContentBytes.ReadAllAsync(request.Content);
                if (body != null && body.Length > 0)
                {
                    unityRequest.uploadHandler = new UploadHandlerRaw(body);
                    var mediaType = request.Content.Headers.ContentType?.MediaType;
                    if (!string.IsNullOrEmpty(mediaType))
                    {
                        unityRequest.uploadHandler.contentType = mediaType;
                    }
                }
            }

            foreach (var header in request.Headers)
            {
                TrySetHeader(unityRequest, header.Key, string.Join(",", header.Value));
            }

            var operation = unityRequest.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    unityRequest.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }

            var status = unityRequest.responseCode > 0
                ? (HttpStatusCode)unityRequest.responseCode
                : HttpStatusCode.ServiceUnavailable;

            var payload = unityRequest.downloadHandler != null
                ? unityRequest.downloadHandler.data
                : Array.Empty<byte>();

            var response = new HttpResponseMessage(status)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(payload ?? Array.Empty<byte>())
            };

            unityRequest.Dispose();
            return response;
        }

        static void TrySetHeader(UnityWebRequest request, string key, string value)
        {
            try
            {
                request.SetRequestHeader(key, value);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
#endif
