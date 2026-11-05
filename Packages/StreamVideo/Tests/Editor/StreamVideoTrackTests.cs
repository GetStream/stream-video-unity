#if STREAM_TESTS_ENABLED
using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using StreamVideo.Core.StatefulModels.Tracks;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using UiImage = UnityEngine.UIElements.Image;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="StreamVideoTrack"/> render targets.
    /// A sending <see cref="VideoStreamTrack"/> has a stable destination texture until its source size changes.
    /// </summary>
    internal sealed class StreamVideoTrackTests
    {
        [SetUp]
        public void SetUp()
        {
            _uiImage = new UiImage();
            _rawHost = new GameObject("StreamVideoTrackTests");
            _rawImage = _rawHost.AddComponent<RawImage>();
            EnsureWebRtcSyncContext();

            try
            {
                _source = CreateTexture(160, 90);
                _mediaTrack = new VideoStreamTrack(_source);
                _track = new StreamVideoTrack(_mediaTrack);
            }
            catch (Exception exception)
            {
                Assert.Ignore(
                    "A sending VideoStreamTrack cannot be constructed in this Editor session (" +
                    exception.GetType().Name + ": " + exception.Message +
                    "). WebRTC.Context is created on assembly reload and is not publicly initializable.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            _track?.Dispose();
            _mediaTrack?.Dispose();
            DestroyTexture(_source);
            DestroyTexture(_replacement);
            if (_rawHost != null)
            {
                Object.DestroyImmediate(_rawHost);
            }
        }

        [Test]
        public void When_ui_toolkit_render_target_set_expect_image_receives_texture()
        {
            _track.SetRenderTarget(_uiImage);

            _track.Update();

            Assert.That(_uiImage.image, Is.SameAs(_mediaTrack.Texture),
                "The UI Toolkit image should show the track texture after Update.");
            Assert.That(_uiImage.image.width, Is.GreaterThan(0),
                "The received texture should have a non-zero width.");
        }

        [Test]
        public void When_retarget_to_raw_image_expect_ui_toolkit_updates_stop()
        {
            _track.SetRenderTarget(_uiImage);
            _track.Update();
            var firstTexture = _mediaTrack.Texture;

            _track.SetRenderTarget(_rawImage);
            _replacement = CreateTexture(320, 180);
            _mediaTrack.ReplaceSourceTexture(_replacement);
            _track.Update();

            Assert.That(_mediaTrack.Texture, Is.Not.SameAs(firstTexture),
                "A source size change should replace the sending texture instance.");
            Assert.That(_rawImage.texture, Is.SameAs(_mediaTrack.Texture),
                "The RawImage should receive the new texture after retargeting.");
            Assert.That(_uiImage.image, Is.SameAs(firstTexture),
                "The UI Toolkit image should stop updating after a RawImage target is set.");
        }

        [Test]
        public void When_retarget_to_ui_toolkit_expect_raw_image_updates_stop()
        {
            _track.SetRenderTarget(_rawImage);
            _track.Update();
            var firstTexture = _mediaTrack.Texture;

            _track.SetRenderTarget(_uiImage);
            _replacement = CreateTexture(320, 180);
            _mediaTrack.ReplaceSourceTexture(_replacement);
            _track.Update();

            Assert.That(_uiImage.image, Is.SameAs(_mediaTrack.Texture),
                "The UI Toolkit image should receive the new texture after retargeting.");
            Assert.That(_rawImage.texture, Is.SameAs(firstTexture),
                "The RawImage should stop updating after a UI Toolkit target is set.");
        }

        [Test]
        public void When_set_render_target_null_expect_argument_null_exception()
        {
            Assert.Throws<ArgumentNullException>(() => _track.SetRenderTarget((RawImage)null),
                "A null RawImage should be rejected.");
            Assert.Throws<ArgumentNullException>(() => _track.SetRenderTarget((UiImage)null),
                "A null UI Toolkit image should be rejected.");
        }

        [Test]
        public void When_track_disposed_expect_ui_toolkit_image_cleared()
        {
            _track.SetRenderTarget(_uiImage);
            _track.Update();
            Assert.That(_uiImage.image, Is.SameAs(_mediaTrack.Texture),
                "Precondition: the image shows this track before dispose.");

            _track.Dispose();
            _track = null;

            Assert.That(_uiImage.image, Is.Null,
                "Dispose should clear the UI Toolkit image when it still shows this track's texture.");
        }

        [Test]
        public void When_disposed_track_image_shows_another_track_expect_texture_kept()
        {
            var otherSource = CreateTexture(192, 108);
            VideoStreamTrack otherMedia = null;
            StreamVideoTrack otherTrack = null;
            try
            {
                otherMedia = new VideoStreamTrack(otherSource);
                otherTrack = new StreamVideoTrack(otherMedia);
                _track.SetRenderTarget(_uiImage);
                _track.Update();
                otherTrack.SetRenderTarget(_uiImage);
                otherTrack.Update();

                _track.Dispose();
                _track = null;

                Assert.That(_uiImage.image, Is.SameAs(otherMedia.Texture),
                    "Dispose should not clear an image that another track has since taken over.");
            }
            finally
            {
                otherTrack?.Dispose();
                otherMedia?.Dispose();
                DestroyTexture(otherSource);
            }
        }

        // Edit Mode does not run RuntimeInitializeOnLoadMethod, so WebRTC's sync context stays null
        // and disposing a track throws. Play Mode installs the real context. This only fills the gap.
        private static void EnsureWebRtcSyncContext()
        {
            var field = typeof(WebRTC).GetField("s_syncContext", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null || field.GetValue(null) != null)
            {
                return;
            }

            field.SetValue(null, new InlineSynchronizationContext());
        }

        private sealed class InlineSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback callback, object state) => callback(state);

            public override void Send(SendOrPostCallback callback, object state) => callback(state);
        }

        private static RenderTexture CreateTexture(int width, int height)
        {
            var format = WebRTC.GetSupportedGraphicsFormat(SystemInfo.graphicsDeviceType);
            var texture = new RenderTexture(width, height, 0, format);
            texture.Create();
            return texture;
        }

        private static void DestroyTexture(RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            texture.Release();
            Object.DestroyImmediate(texture);
        }

        private StreamVideoTrack _track;
        private VideoStreamTrack _mediaTrack;
        private RenderTexture _source;
        private RenderTexture _replacement;
        private GameObject _rawHost;
        private RawImage _rawImage;
        private UiImage _uiImage;
    }
}
#endif
