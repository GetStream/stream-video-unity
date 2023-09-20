using System;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Provides connection id
    /// </summary>
    public interface IConnectionProvider
    {
        Uri ServerUri { get; }
        string ConnectionId { get; }
    }
}