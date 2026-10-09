#if STREAM_TESTS_ENABLED
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Runtime
{
    /// <summary>
    /// Tests for <see cref="IStreamCall"/> background filter API.
    /// </summary>
    internal class BackgroundFilterTests : TestsBase
    {
        [UnityTest]
        public IEnumerator When_setting_background_filter_expect_no_throw_and_leave_succeeds()
            => ConnectAndExecute(When_setting_background_filter_expect_no_throw_and_leave_succeeds_Async);

        private async Task When_setting_background_filter_expect_no_throw_and_leave_succeeds_Async(ITestClient client)
        {
            var call = await client.JoinRandomCallAsync();

            Assert.DoesNotThrow(() => call.SetBackgroundFilter(BackgroundFilter.Blur(BlurIntensity.Medium)),
                "SetBackgroundFilter must not throw, including when the platform is unsupported.");

            if (!call.IsBackgroundFilterSupported)
            {
                Assert.That(call.ActiveBackgroundFilter, Is.Null,
                    "Unsupported platforms should leave the filter disabled.");
            }

            Assert.DoesNotThrow(() => call.SetBackgroundFilter(null),
                "Clearing the background filter must not throw.");

            await call.LeaveAsync();
        }

        [UnityTest]
        public IEnumerator When_toggling_background_filter_expect_same_local_preview_texture_until_leave()
            => ConnectAndExecute(When_toggling_background_filter_expect_same_local_preview_texture_until_leave_Async);

        private async Task When_toggling_background_filter_expect_same_local_preview_texture_until_leave_Async(
            ITestClient client)
        {
            var firstCall = await client.JoinRandomCallAsync();

            var preview = firstCall.GetLocalPreviewTexture();
            Assert.That(preview, Is.Not.Null,
                "Local preview must be available right after join, even before the camera delivers frames.");

            firstCall.SetBackgroundFilter(BackgroundFilter.Blur(BlurIntensity.Medium));
            Assert.That(firstCall.GetLocalPreviewTexture(), Is.SameAs(preview),
                "Enabling a background filter must not replace the preview instance.");

            firstCall.SetBackgroundFilter(null);
            Assert.That(firstCall.GetLocalPreviewTexture(), Is.SameAs(preview),
                "Disabling a background filter must not replace the preview instance.");

            await firstCall.LeaveAsync();

            // Object.Destroy is deferred to the end of the frame in play mode
            var (isDestroyed, _) = await WaitForConditionAsync(() => preview == null);
            Assert.That(isDestroyed, Is.True, "Leaving the call must destroy the preview texture.");
            Assert.That(firstCall.GetLocalPreviewTexture(), Is.Null,
                "A call that is no longer active must not return a preview texture.");

            var secondCall = await client.JoinRandomCallAsync();
            var secondPreview = secondCall.GetLocalPreviewTexture();
            Assert.That(secondPreview, Is.Not.Null, "The next call must provide its own preview texture.");
            Assert.That(ReferenceEquals(secondPreview, preview), Is.False,
                "The next call must not reuse the destroyed preview texture.");

            await secondCall.LeaveAsync();
        }

        [UnityTest]
        public IEnumerator When_subscribing_performance_event_then_leave_expect_handler_not_invoked_on_next_call()
            => ConnectAndExecute(When_subscribing_performance_event_then_leave_expect_handler_not_invoked_on_next_call_Async);

        private async Task When_subscribing_performance_event_then_leave_expect_handler_not_invoked_on_next_call_Async(
            ITestClient client)
        {
            var firstCall = await client.JoinRandomCallAsync();
            var invocations = 0;
            firstCall.BackgroundFilterPerformanceChanged += _ => invocations++;

            await firstCall.LeaveAsync();

            var secondCall = await client.JoinRandomCallAsync();
            secondCall.SetBackgroundFilter(BackgroundFilter.Blur(BlurIntensity.Medium));
            secondCall.SetBackgroundFilter(null);

            Assert.That(invocations, Is.EqualTo(0),
                "Handlers subscribed on the first call must not fire after Leave or on the next call.");

            await secondCall.LeaveAsync();
        }
    }
}
#endif
