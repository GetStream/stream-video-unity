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
    }
}
#endif
