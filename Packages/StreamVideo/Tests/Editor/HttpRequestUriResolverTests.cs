#if STREAM_TESTS_ENABLED
using System;
using System.Net.Http;
using NUnit.Framework;
using StreamVideo.Libs.Http;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="HttpRequestUriResolver"/>.
    /// </summary>
    internal sealed class HttpRequestUriResolverTests
    {
        [Test]
        public void When_twirp_http_request_expect_https_sfu_url()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TwirpPath);
            var resolved = HttpRequestUriResolver.Resolve(SfuBaseAddress, request.RequestUri);

            Assert.That(resolved.IsAbsoluteUri, Is.True,
                "Twirp paths must resolve to an absolute http(s) URL before UnityWebRequest.");
            Assert.That(resolved.Scheme, Is.EqualTo(Uri.UriSchemeHttps),
                "Twirp paths must not stay as file:///twirp/... in the WebGL player.");
            Assert.That(resolved.AbsoluteUri, Is.EqualTo(ExpectedHttpsUrl),
                "SetPublisher/UpdateMuteStates must hit the SFU host, not a local file URI.");
        }

        [Test]
        public void When_il2cpp_file_uri_expect_https_sfu_url()
        {
            var fileUri = new Uri("file:///twirp/stream.video.sfu.signal.SignalServer/SetPublisher");
            var resolved = HttpRequestUriResolver.Resolve(SfuBaseAddress, fileUri);

            Assert.That(fileUri.IsAbsoluteUri, Is.True,
                "Precondition: Mono/IL2CPP treats /twirp/... as an absolute file URI.");
            Assert.That(fileUri.IsFile, Is.True,
                "Precondition: the misparsed Twirp path must be a file URI.");
            Assert.That(resolved.AbsoluteUri, Is.EqualTo(ExpectedHttpsUrl),
                "new Uri(base, fileUri) keeps the file URI because the second argument is absolute. The path must be combined as a relative string.");
        }

        [Test]
        public void When_https_request_expect_unchanged()
        {
            var https = new Uri(ExpectedHttpsUrl);
            var resolved = HttpRequestUriResolver.Resolve(SfuBaseAddress, https);

            Assert.That(resolved, Is.EqualTo(https),
                "Already-absolute http(s) SFU URLs must not be rewritten.");
        }

        [Test]
        public void When_null_request_expect_base_address()
        {
            var resolved = HttpRequestUriResolver.Resolve(SfuBaseAddress, null);

            Assert.That(resolved, Is.EqualTo(SfuBaseAddress),
                "A missing request URI should fall back to HttpClient.BaseAddress.");
        }

        [Test]
        public void When_missing_base_address_expect_throw()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TwirpPath);

            Assert.That(() => HttpRequestUriResolver.Resolve(null, request.RequestUri),
                Throws.InvalidOperationException,
                "Relative Twirp paths cannot be sent without HttpClient.BaseAddress.");
        }

        static readonly Uri SfuBaseAddress = new Uri("https://sfu.example.com");
        const string TwirpPath = "/twirp/stream.video.sfu.signal.SignalServer/SetPublisher";
        const string ExpectedHttpsUrl = "https://sfu.example.com/twirp/stream.video.sfu.signal.SignalServer/SetPublisher";
    }
}
#endif
