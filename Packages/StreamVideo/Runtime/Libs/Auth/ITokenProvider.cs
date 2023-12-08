using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace StreamVideo.Libs.Auth
{
    //StreamTodo: write docs about the token provider
    /// <summary>
    /// Providers JWT authorization token for Stream Chat
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>
        /// Get JWT token for the provided user id
        /// </summary>
        /// <remarks>https://getstream.io/chat/docs/unity/tokens_and_authentication/?language=unity#token-providers</remarks>
        Task<string> GetTokenAsync(string userId, CancellationToken cancellationToken = default);
    }
}