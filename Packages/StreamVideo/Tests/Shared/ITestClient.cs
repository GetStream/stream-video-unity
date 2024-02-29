using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Tests.Shared
{
    public interface ITestClient
    {
        IStreamVideoClient Client { get; }

        Task<IStreamCall> JoinRandomCallAsync();

        Task CleanupAfterSingleTestSessionAsync();

        Task ConnectAsync();
    }
}