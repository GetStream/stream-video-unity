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
        public IEnumerator When_toggling_background_filter_and_rejoining_expect_same_local_preview_texture()
            => ConnectAndExecute(When_toggling_background_filter_and_rejoining_expect_same_local_preview_texture_Async);

        private async Task When_toggling_background_filter_and_rejoining_expect_same_local_preview_texture_Async(
            ITestClient client)
        {
            var deviceManager = client.Client.VideoDeviceManager;

            var preview = deviceManager.GetLocalPreviewTexture();
            Assert.That(preview, Is.Not.Null,
                "Local preview must be available before joining, even before the camera delivers frames.");

            var firstCall = await client.JoinRandomCallAsync();
            Assert.That(deviceManager.GetLocalPreviewTexture(), Is.SameAs(preview),
                "Joining a call must not replace the preview instance.");

            firstCall.SetBackgroundFilter(BackgroundFilter.Blur(BlurIntensity.Medium));
            Assert.That(deviceManager.GetLocalPreviewTexture(), Is.SameAs(preview),
                "Enabling a background filter must not replace the preview instance.");

            firstCall.SetBackgroundFilter(null);
            Assert.That(deviceManager.GetLocalPreviewTexture(), Is.SameAs(preview),
                "Disabling a background filter must not replace the preview instance.");

            await firstCall.LeaveAsync();
            Assert.That(preview != null, Is.True, "Leaving a call must not destroy the preview texture.");
            Assert.That(deviceManager.GetLocalPreviewTexture(), Is.SameAs(preview),
                "The preview must stay the same after leaving so the pre-call UI keeps working.");

            var secondCall = await client.JoinRandomCallAsync();
            Assert.That(deviceManager.GetLocalPreviewTexture(), Is.SameAs(preview),
                "The next call must reuse the same preview instance.");

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
