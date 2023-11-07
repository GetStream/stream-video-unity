using StreamVideo.Core;
using StreamVideo.Libs.Auth;

namespace DocsCodeSamples.Other
{
    internal class GithubRepoCodeExamples
    {
        public async void PromoImageSetup()
        {
            var credentials = new AuthCredentials("api-key", "user-id", "user-token");
            var callId = "my-call-id";
            
            var client = StreamVideoClient.CreateDefaultClient();
            await client.ConnectUserAsync(credentials);
            await client.JoinCallAsync(StreamCallType.Default, callId, create: true, ring: true, notify: false);
        }
    }
}