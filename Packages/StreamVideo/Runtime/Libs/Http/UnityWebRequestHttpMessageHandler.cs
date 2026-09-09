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
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var unityRequest = new UnityWebRequest(request.RequestUri.AbsoluteUri, request.Method.Method)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };

            if (request.Content != null)
            {
                var body = await request.Content.ReadAsByteArrayAsync();
                if (body != null && body.Length > 0)
                {
                    unityRequest.uploadHandler = new UploadHandlerRaw(body);
                    if (request.Content.Headers.ContentType != null)
                    {
                        unityRequest.uploadHandler.contentType = request.Content.Headers.ContentType.ToString();
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
