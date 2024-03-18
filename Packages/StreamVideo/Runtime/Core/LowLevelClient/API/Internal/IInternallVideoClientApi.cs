using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;

namespace StreamVideo.Core.LowLevelClient.API.Internal
{
    internal interface IInternalVideoClientApi
    {
        Task<GetCallResponseInternalDTO> GetCallAsync(StreamCallType callType, string callId,
            GetOrCreateCallRequestInternalDTO getCallRequest);

        Task<UpdateCallResponseInternalDTO> UpdateCallAsync(StreamCallType callType, string callId,
            UpdateCallRequestInternalDTO updateCallRequest);

        Task<GetOrCreateCallResponseInternalDTO> GetOrCreateCallAsync(StreamCallType callType, string callId,
            GetOrCreateCallRequestInternalDTO getOrCreateCallRequest);

        Task<AcceptCallResponseInternalDTO> AcceptCallAsync(StreamCallType callType, string callId);

        Task<BlockUserResponseInternalDTO> BlockUserAsync(StreamCallType callType, string callId,
            BlockUserRequestInternalDTO blockUserRequest);

        Task<UnblockUserResponseInternalDTO> UnblockUserAsync(StreamCallType callType, string callId,
            UnblockUserRequestInternalDTO unblockUserRequest);

        Task<SendEventResponseInternalDTO> SendEventAsync(StreamCallType callType, string callId,
            SendEventRequestInternalDTO sendEventRequest);

        Task<GoLiveResponseInternalDTO> GoLiveAsync(StreamCallType callType, string callId);

        Task<StopLiveResponseInternalDTO> StopLiveAsync(StreamCallType callType, string callId);

        Task<JoinCallResponseInternalDTO> JoinCallAsync(StreamCallType callType, string callId,
            JoinCallRequestInternalDTO joinCallRequest);

        Task<EndCallResponseInternalDTO> EndCallAsync(StreamCallType callType, string callId);

        Task<UpdateCallMembersResponseInternalDTO> UpdateCallMembersAsync(StreamCallType callType, string callId,
            UpdateCallMembersRequestInternalDTO updateCallMembersRequest);

        Task<MuteUsersResponseInternalDTO> MuteUsersAsync(StreamCallType callType, string callId,
            MuteUsersRequestInternalDTO muteUsersRequest);

        Task<SendReactionResponseInternalDTO> SendVideoReactionAsync(StreamCallType callType, string callId,
            SendReactionRequestInternalDTO sendReactionRequest);

        Task<RejectCallResponseInternalDTO> RejectCallAsync(StreamCallType callType, string callId);

        Task<RequestPermissionResponseInternalDTO> RequestPermissionAsync(StreamCallType callType, string callId,
            RequestPermissionRequestInternalDTO requestPermissionRequest);

        Task<UpdateUserPermissionsResponseInternalDTO> UpdateUserPermissionsAsync(StreamCallType callType, string callId,
            UpdateUserPermissionsRequestInternalDTO updateUserPermissionsRequest);

        Task<StartHLSBroadcastingResponseInternalDTO> StartBroadcastingAsync(StreamCallType callType, string callId);

        Task<StopHLSBroadcastingResponseInternalDTO> StopBroadcastingAsync(StreamCallType callType, string callId);

        Task<StartRecordingResponseInternalDTO> StartRecordingAsync(StreamCallType callType, string callId);

        Task<StopRecordingResponseInternalDTO> StopRecordingAsync(StreamCallType callType, string callId);

        Task<StartTranscriptionResponseInternalDTO> StartTranscriptionAsync(StreamCallType callType, string callId);

        Task<StopTranscriptionResponseInternalDTO> StopTranscriptionAsync(StreamCallType callType, string callId);

        Task<QueryMembersResponseInternalDTO> QueryMembersAsync(QueryMembersRequestInternalDTO queryMembersRequest);

        Task<QueryCallsResponseInternalDTO> QueryCallsAsync(QueryCallsRequestInternalDTO queryCallsRequest);

        Task<ResponseInternalDTO> DeleteDeviceAsync(string deviceId, string userId);

        Task<ListDevicesResponseInternalDTO> ListDevicesAsync(string userId);

        Task<ResponseInternalDTO> CreateDeviceAsync(CreateDeviceRequestInternalDTO createDeviceRequest);

        Task<GetEdgesResponseInternalDTO> GetEdgesAsync();

        Task<CreateGuestResponseInternalDTO> CreateGuestAsync(CreateGuestRequestInternalDTO createGuestRequest);

        Task<ResponseInternalDTO> VideoConnectAsync(WSAuthMessageRequestInternalDTO authMessageRequest);
    }
}