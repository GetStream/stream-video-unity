using System;
using System.Collections.Generic;
using System.Text;
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

        public Task<HttpResponse> GetAsync(Uri uri) => SendWebRequest(uri, UnityWebRequest.kHttpVerbGET);

        public Task<HttpResponse> PostAsync(Uri uri, object content)
            => SendWebRequest(uri, UnityWebRequest.kHttpVerbPOST, content);

        public Task<HttpResponse> PutAsync(Uri uri, object content)
            => SendWebRequest(uri, UnityWebRequest.kHttpVerbPUT, content);

        public Task<HttpResponse> PatchAsync(Uri uri, object content) => SendWebRequest(uri, HttpPatchMethod, content);

        public Task<HttpResponse> DeleteAsync(Uri uri) => SendWebRequest(uri, UnityWebRequest.kHttpVerbDELETE);

        public Task<HttpResponse> SendHttpRequestAsync(HttpMethodType methodType, Uri uri,
            object optionalRequestContent)
        {
            var httpMethodKey = GetHttpMethodKey(methodType);
            return SendWebRequest(uri, httpMethodKey, optionalRequestContent);
        }

        public Task<HttpResponse> HeadAsync(Uri uri,
            ICollection<KeyValuePair<string, IEnumerable<string>>> resultHeaders)
            => SendWebRequest(uri, HttpHeadMethod, resultHeaders: resultHeaders);

        private const string HttpHeadMethod = "HEAD";
        private const string HttpPatchMethod = "PATCH";

        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

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
            object optionalContent = null, ICollection<KeyValuePair<string, IEnumerable<string>>> resultHeaders = null)
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
                unityWebRequest.timeout = 5;

                foreach (var pair in _headers)
                {
                    unityWebRequest.SetRequestHeader(pair.Key, pair.Value);
                }

                var asyncOperation = unityWebRequest.SendWebRequest();

                while (!asyncOperation.isDone)
                {
                    await Task.Yield();
                }

                if (unityWebRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception(unityWebRequest.error);
                }

                if (resultHeaders != null)
                {
                    foreach (var header in unityWebRequest.GetResponseHeaders())
                    {
                        resultHeaders.Add(
                            new KeyValuePair<string, IEnumerable<string>>(header.Key, new string[] { header.Value }));
                    }
                }

                return HttpResponse.CreateFromUnityWebRequest(unityWebRequest);
            }

            using (var unityWebRequest = new UnityWebRequest(uri, httpMethod))
            {
                if (optionalContent == null)
                {
                }
                else if (optionalContent is string stringContent)
                {
                    unityWebRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(stringContent));
                }
                else
                {
                    throw new NotImplementedException(
                        $"Not implemented support for body object type of {optionalContent.GetType()}");
                }

                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
                unityWebRequest.timeout = 5;

                foreach (var pair in _headers)
                {
                    unityWebRequest.SetRequestHeader(pair.Key, pair.Value);
                }

                var asyncOperation = unityWebRequest.SendWebRequest();

                while (!asyncOperation.isDone)
                {
                    await Task.Yield();
                }

                if (unityWebRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception(unityWebRequest.error);
                }

                if (resultHeaders != null)
                {
                    foreach (var header in unityWebRequest.GetResponseHeaders())
                    {
                        resultHeaders.Add(
                            new KeyValuePair<string, IEnumerable<string>>(header.Key, new string[] { header.Value }));
                    }
                }

                return HttpResponse.CreateFromUnityWebRequest(unityWebRequest);
            }
        }
    }
}