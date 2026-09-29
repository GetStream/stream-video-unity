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
        public IEnumerator When_subscribing_preview_event_then_leave_expect_handler_not_invoked_on_next_call()
            => ConnectAndExecute(When_subscribing_preview_event_then_leave_expect_handler_not_invoked_on_next_call_Async);

        private async Task When_subscribing_preview_event_then_leave_expect_handler_not_invoked_on_next_call_Async(
            ITestClient client)
        {
            var firstCall = await client.JoinRandomCallAsync();
            var invocations = 0;
            firstCall.LocalPreviewTextureChanged += _ => invocations++;
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
