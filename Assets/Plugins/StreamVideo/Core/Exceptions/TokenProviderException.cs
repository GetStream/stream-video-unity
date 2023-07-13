using System;
using StreamVideo.Libs.Auth;

namespace StreamVideo.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the injected <see cref="ITokenProvider"/> fails to return a token
    /// </summary>
    public class TokenProviderException : Exception
    {
        public TokenProviderException(string message)
            : base(message)
        {
        }
        
        public TokenProviderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}