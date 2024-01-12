using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core
{
    internal interface IInternalStreamVideoClient
    {
        StreamVideoLowLevelClient InternalLowLevelClient { get; }

        Task LeaveCallAsync(IStreamCall call);

        Task EndCallAsync(IStreamCall call);

        Task StartHLSAsync(IStreamCall call);

        Task StopHLSAsync(IStreamCall call);

        Task GoLiveAsync(IStreamCall call);

        Task StopLiveAsync(IStreamCall call);

        Task StartRecordingAsync(IStreamCall call);

        Task StopRecordingAsync(IStreamCall call);

        Task MuteAllUsersAsync(IStreamCall call, bool audio, bool video, bool screenShare);

        Task BlockUserAsync(IStreamCall call, string userId);

        Task UnblockUserAsync(IStreamCall call, string userId);

        Task RequestPermissionAsync(IStreamCall call, List<string> capabilities);

        Task UpdateUserPermissions(IStreamCall call, string userId, List<string> grantPermissions,
            List<string> revokePermissions);

        Task RemoveMembersAsync(IStreamCall call, List<string> removeUsers);

        Task SetParticipantCustomDataAsync(IStreamVideoCallParticipant participant,
            Dictionary<string, object> internalDictionary);
    }
}