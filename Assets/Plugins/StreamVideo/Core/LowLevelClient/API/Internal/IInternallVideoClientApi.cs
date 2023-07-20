using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;

namespace StreamVideo.Core.LowLevelClient.API.Internal
{
    internal interface IInternalVideoClientApi
    {
        Task<GetCallResponse> GetCallAsync(GetOrCreateCallRequest getCallRequest);

        Task<UpdateCallResponse> UpdateCallAsync(StreamCallType callType, string callId,
            UpdateCallRequest updateCallRequest);

        Task<GetOrCreateCallResponse> GetOrCreateCallAsync(StreamCallType callType, string callId,
            GetOrCreateCallRequest getOrCreateCallRequest);

        Task<AcceptCallResponse> AcceptCallAsync(StreamCallType callType, string callId);

        Task<BlockUserResponse> BlockUserAsync(StreamCallType callType, string callId,
            BlockUserRequest blockUserRequest);

        Task<UnblockUserResponse> UnblockUserAsync(StreamCallType callType, string callId,
            UnblockUserRequest unblockUserRequest);

        Task<SendEventResponse> SendEventAsync(StreamCallType callType, string callId,
            SendEventRequest sendEventRequest);

        Task<GoLiveResponse> GoLiveAsync(StreamCallType callType, string callId);

        Task<StopLiveResponse> StopLiveAsync(StreamCallType callType, string callId);

        Task<JoinCallResponse> JoinCallAsync(StreamCallType callType, string callId,
            JoinCallRequest joinCallRequest);

        Task<EndCallResponse> EndCallAsync(StreamCallType callType, string callId);

        Task<UpdateCallMembersResponse> UpdateCallMembersAsync(StreamCallType callType, string callId,
            UpdateCallMembersRequest updateCallMembersRequest);

        Task<MuteUsersResponse> MuteUsersAsync(StreamCallType callType, string callId,
            MuteUsersRequest muteUsersRequest);

        Task<SendReactionResponse> SendVideoReactionAsync(StreamCallType callType, string callId,
            SendReactionRequest sendReactionRequest);

        Task<RejectCallResponse> RejectCallAsync(StreamCallType callType, string callId);

        Task<RequestPermissionResponse> RequestPermissionAsync(StreamCallType callType, string callId,
            RequestPermissionRequest requestPermissionRequest);

        Task<UpdateUserPermissionsResponse> UpdateUserPermissionsAsync(StreamCallType callType, string callId,
            UpdateUserPermissionsRequest updateUserPermissionsRequest);

        Task<StartBroadcastingResponse> StartBroadcastingAsync(StreamCallType callType, string callId);

        Task<StopBroadcastingResponse> StopBroadcastingAsync(StreamCallType callType, string callId);

        Task<StartRecordingResponse> StartRecordingAsync(StreamCallType callType, string callId);

        Task<StopRecordingResponse> StopRecordingAsync(StreamCallType callType, string callId);

        Task<StartTranscriptionResponse> StartTranscriptionAsync(StreamCallType callType, string callId);

        Task<StopTranscriptionResponse> StopTranscriptionAsync(StreamCallType callType, string callId);

        Task<QueryMembersResponse> QueryMembersAsync(QueryMembersRequest queryMembersRequest);

        Task<QueryCallsResponse> QueryCallsAsync(StreamCallType callType, string callId,
            QueryCallsRequest queryCallsRequest);

        Task<Response> DeleteDeviceAsync(string deviceId, string userId);

        Task<ListDevicesResponse> ListDevicesAsync(string userId);

        Task<Response> CreateDeviceAsync(CreateDeviceRequest createDeviceRequest);

        Task<GetEdgesResponse> GetEdgesAsync();

        Task<CreateGuestResponse> CreateGuestAsync(CreateGuestRequest createGuestRequest);

        Task<Response> VideoConnectAsync(WSAuthMessageRequest authMessageRequest);
    }
}