#if STREAM_TESTS_ENABLED
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="IStreamVideoClient"/>
    /// </summary>
    internal class StreamClientTests : TestsBase
    {
        [Test]
        public void When_creating_the_client_expect_no_errors()
        {
            var client = StreamVideoClient.CreateDefaultClient();
        }
        
        [UnityTest]
        public IEnumerator When_connecting_user_expect_no_errors()
            => ConnectAndExecute(When_connecting_user_expect_no_errors_Async);

        private Task When_connecting_user_expect_no_errors_Async(ITestClient client)
        {
            Assert.IsTrue(client.Client.IsConnected);
            return Task.CompletedTask;
        }

        //StreamTodo: ensure that LocalUser is populated when connected is triggered
    }
}
#endif