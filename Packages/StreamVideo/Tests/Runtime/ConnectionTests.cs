using System.Collections;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Libs.Auth;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Runtime
{
    internal class ConnectionTests : TestsBase
    {
        // StreamTODO: figure out passing specific api key to this test (with enabled "disable auth checks" flag)
        //[UnityTest]
        public IEnumerator When_connecting_developer_app_with_app_tokens_expect_no_issues()
        {
            yield return Execute(When_connecting_developer_app_with_app_tokens_expect_no_issues_Async);
        }

        private async Task When_connecting_developer_app_with_app_tokens_expect_no_issues_Async()
        {
            // Only API KEY with "Disable Auth Checks" flag enabled will work here
            const string apiKey = "API_KEY";

            var userName = "The Amazing Tom";
            var userId = StreamVideoClient.SanitizeUserId(userName);
            var userToken = StreamVideoClient.CreateDeveloperAuthToken(userId);
            var credentials = new AuthCredentials(apiKey, userId, userToken);

            var client = StreamVideoClient.CreateDefaultClient();

            var localUserData = await client.ConnectUserAsync(credentials);
        }
    }
}