using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace StreamVideo.Tests
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

        private Task When_connecting_user_expect_no_errors_Async()
        {
            var client = Client;
            Assert.IsTrue(client.IsConnected);

            return Task.CompletedTask;
        }

        //StreamTodo: ensure that LocalUser is populated when connected is triggered
    }
}