using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace StreamVideo.Libs.Http
{
    /// <summary>
    /// <see cref="IHttpClient"/> implementation using <see cref="UnityWebRequest"/>
    /// </summary>
    public class UnityWebRequestHttpClient : IHttpClient
    {
        public void SetDefaultAuthenticationHeader(string value) => _headers["Authorization"] = value;

        public void AddDefaultCustomHeader(string key, string value) => _headers[key] = value;

        public Task<HttpResponse> GetAsync(Uri uri, CancellationToken cancellationToken = default)
            => SendWebRequest(uri, UnityWebRequest.kHttpVerbGET, cancellationToken: cancellationToken);

        public Task<HttpResponse> PostAsync(Uri uri, object content, CancellationToken cancellationToken = default)
            => SendWebRequest(uri, UnityWebRequest.kHttpVerbPOST, content, cancellationToken: cancellationToken);

        public Task<HttpResponse> PutAsync(Uri uri, object content, CancellationToken cancellationToken = default)
            => SendWebRequest(uri, UnityWebRequest.kHttpVerbPUT, content, cancellationToken: cancellationToken);

        public Task<HttpResponse> PatchAsync(Uri uri, object content, CancellationToken cancellationToken = default)
            => SendWebRequest(uri, HttpPatchMethod, content, cancellationToken: cancellationToken);

        public Task<HttpResponse> DeleteAsync(Uri uri, CancellationToken cancellationToken = default)
            => SendWebRequest(uri, UnityWebRequest.kHttpVerbDELETE, cancellationToken: cancellationToken);

        public Task<HttpResponse> SendHttpRequestAsync(HttpMethodType methodType, Uri uri,
            object optionalRequestContent, CancellationToken cancellationToken = default)
        {
            var httpMethodKey = GetHttpMethodKey(methodType);
            return SendWebRequest(uri, httpMethodKey, optionalRequestContent, cancellationToken: cancellationToken);
        }

        public Task<HttpResponse> HeadAsync(Uri uri,
            ICollection<KeyValuePair<string, IEnumerable<string>>> resultHeaders, CancellationToken cancellationToken)
            => SendWebRequest(uri, HttpHeadMethod, resultHeaders: resultHeaders, cancellationToken: cancellationToken,
                includeDefaultHeaders: false);

        private const string HttpHeadMethod = "HEAD";
        private const string HttpPatchMethod = "PATCH";
        private const string JsonContentType = "application/json";
        // Native HttpClient defaults to 100s. 5s is too short for WebGL: CORS preflight + POST
        // share one timer, and a timed-out XHR surfaces only as "Unknown Error".
        private const int RequestTimeoutSeconds = 60;

        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

        private static bool MethodAllowsRequestBody(string httpMethod)
            => httpMethod != UnityWebRequest.kHttpVerbGET
               && httpMethod != UnityWebRequest.kHttpVerbHEAD
               && httpMethod != HttpHeadMethod
               && httpMethod != UnityWebRequest.kHttpVerbDELETE;

        private static string GetHttpMethodKey(HttpMethodType methodType)
        {
            switch (methodType)
            {
                case HttpMethodType.Get: return UnityWebRequest.kHttpVerbGET;
                case HttpMethodType.Post: return UnityWebRequest.kHttpVerbPOST;
                case HttpMethodType.Put: return UnityWebRequest.kHttpVerbPUT;
                case HttpMethodType.Patch: return "PATCH";
                case HttpMethodType.Delete: return UnityWebRequest.kHttpVerbDELETE;
                default:
                    throw new ArgumentOutOfRangeException(nameof(methodType), methodType, null);
            }
        }

        //StreamTodo: add cancellationToken support that will trigger https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.Abort.html
        //StreamTodo: refactor to remove duplication
        private async Task<HttpResponse> SendWebRequest(Uri uri, string httpMethod,
            object optionalContent = null, ICollection<KeyValuePair<string, IEnumerable<string>>> resultHeaders = null,
            CancellationToken cancellationToken = default, bool includeDefaultHeaders = true)
        {
            if (optionalContent is FileWrapper fileWrapper)
            {
                var formData = new List<IMultipartFormSection>
                {
                    new MultipartFormFileSection("file", fileWrapper.FileContent, fileWrapper.FileName,
                        "multipart/form-data")
                };

                var unityWebRequest = UnityWebRequest.Post(uri, formData);

                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();

                ApplyDefaultHeaders(unityWebRequest, includeDefaultHeaders);

                await SendAndWait(unityWebRequest, uri, httpMethod, cancellationToken);

                CopyResponseHeaders(unityWebRequest, resultHeaders);

                return HttpResponse.CreateFromUnityWebRequest(unityWebRequest);
            }

            using (var unityWebRequest = new UnityWebRequest(uri, httpMethod))
            {
                if (MethodAllowsRequestBody(httpMethod) && optionalContent != null)
                {
                    if (optionalContent is string stringContent)
                    {
                        unityWebRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(stringContent))
                        {
                            // Default is application/octet-stream. Stream's API and CORS expect JSON,
                            // matching the web SDK. A 4xx without CORS on the error is reported as
                            // "Unknown Error" in WebGL.
                            contentType = JsonContentType
                        };
                    }
                    else
                    {
                        throw new NotImplementedException(
                            $"Not implemented support for body object type of {optionalContent.GetType()}");
                    }
                }

                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();

                ApplyDefaultHeaders(unityWebRequest, includeDefaultHeaders);

                await SendAndWait(unityWebRequest, uri, httpMethod, cancellationToken);

                CopyResponseHeaders(unityWebRequest, resultHeaders);

                return HttpResponse.CreateFromUnityWebRequest(unityWebRequest);
            }
        }

        private static async Task SendAndWait(UnityWebRequest unityWebRequest, Uri uri, string httpMethod,
            CancellationToken cancellationToken)
        {
            unityWebRequest.timeout = RequestTimeoutSeconds;

            var asyncOperation = unityWebRequest.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            ThrowIfTransportFailed(unityWebRequest, uri, httpMethod);
        }

        private static void ThrowIfTransportFailed(UnityWebRequest unityWebRequest, Uri uri, string httpMethod)
        {
            // ProtocolError (HTTP 4xx/5xx) must be returned so the API layer can parse Stream errors.
            // ConnectionError/DataProcessingError have no usable body; on WebGL this is usually CORS or timeout.
            if (unityWebRequest.result == UnityWebRequest.Result.Success ||
                unityWebRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                return;
            }

            throw new Exception(FormatRequestFailure(unityWebRequest, uri, httpMethod));
        }

        private static string FormatRequestFailure(UnityWebRequest unityWebRequest, Uri uri, string httpMethod)
        {
            var body = unityWebRequest.downloadHandler != null ? unityWebRequest.downloadHandler.text : string.Empty;
            if (body != null && body.Length > 512)
            {
                body = body.Substring(0, 512) + "...";
            }

            return
                $"UnityWebRequest failed: {httpMethod} {uri} result={unityWebRequest.result} " +
                $"error={unityWebRequest.error} status={unityWebRequest.responseCode} body={body}";
        }

        private void ApplyDefaultHeaders(UnityWebRequest unityWebRequest, bool includeDefaultHeaders)
        {
            if (!includeDefaultHeaders)
            {
                return;
            }

            foreach (var pair in _headers)
            {
                try
                {
                    unityWebRequest.SetRequestHeader(pair.Key, pair.Value);
                }
                catch (InvalidOperationException)
                {
                    // WebGL/XHR forbids some headers (User-Agent, etc.). Skip rather than fail the request.
                }
            }
        }

        private static void CopyResponseHeaders(UnityWebRequest unityWebRequest,
            ICollection<KeyValuePair<string, IEnumerable<string>>> resultHeaders)
        {
            if (resultHeaders == null)
            {
                return;
            }

            var responseHeaders = unityWebRequest.GetResponseHeaders();
            if (responseHeaders == null)
            {
                return;
            }

            foreach (var header in responseHeaders)
            {
                resultHeaders.Add(
                    new KeyValuePair<string, IEnumerable<string>>(header.Key, new string[] { header.Value }));
            }
        }
    }
}
