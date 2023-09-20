using System.Threading.Tasks;
using StreamChat.Core.LowLevelClient.API.Internal;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.Web;
using StreamVideo.Libs.Http;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.LowLevelClient.API.Internal
{
    //StreamTodo: add support for cancellation token
    //StreamTodo: add retry strategy logic, e.g. would should probably retry every request 3x by default
    internal class InternalVideoClientApi : InternalApiClientBase, IInternalVideoClientApi
    {
        public InternalVideoClientApi(IHttpClient httpClient, ISerializer serializer, ILogs logs,
            IRequestUriFactory requestUriFactory, IStreamVideoLowLevelClient lowLevelClient) : base(httpClient,
            serializer, logs, requestUriFactory, lowLevelClient)
        {
        }

        public Task<GetCallResponseInternalDTO> GetCallAsync(StreamCallType callType, string callId,
            GetOrCreateCallRequestInternalDTO getCallRequest)
            => Get<GetOrCreateCallRequestInternalDTO, GetCallResponseInternalDTO>($"/call/{callType}/{callId}", getCallRequest);

        public Task<UpdateCallResponseInternalDTO> UpdateCallAsync(StreamCallType callType, string callId,
            UpdateCallRequestInternalDTO updateCallRequest)
            => Patch<UpdateCallRequestInternalDTO, UpdateCallResponseInternalDTO>($"/call/{callType}/{callId}", updateCallRequest);

        public Task<GetOrCreateCallResponseInternalDTO> GetOrCreateCallAsync(StreamCallType callType, string callId,
            GetOrCreateCallRequestInternalDTO getOrCreateCallRequest)
            => Post<GetOrCreateCallRequestInternalDTO, GetOrCreateCallResponseInternalDTO>($"/call/{callType}/{callId}",
                getOrCreateCallRequest);

        public Task<AcceptCallResponseInternalDTO> AcceptCallAsync(StreamCallType callType, string callId)
            => Post<AcceptCallResponseInternalDTO>($"/call/{callType}/{callId}/accept");

        public Task<BlockUserResponseInternalDTO> BlockUserAsync(StreamCallType callType, string callId,
            BlockUserRequestInternalDTO blockUserRequest)
            => Post<BlockUserRequestInternalDTO, BlockUserResponseInternalDTO>($"/call/{callType}/{callId}/block", blockUserRequest);

        public Task<UnblockUserResponseInternalDTO> UnblockUserAsync(StreamCallType callType, string callId,
            UnblockUserRequestInternalDTO unblockUserRequest)
            => Post<UnblockUserRequestInternalDTO, UnblockUserResponseInternalDTO>($"/call/{callType}/{callId}/unblock", unblockUserRequest);

        public Task<SendEventResponseInternalDTO> SendEventAsync(StreamCallType callType, string callId,
            SendEventRequestInternalDTO sendEventRequest)
            => Post<SendEventRequestInternalDTO, SendEventResponseInternalDTO>($"/call/{callType}/{callId}/event", sendEventRequest);

        public Task<GoLiveResponseInternalDTO> GoLiveAsync(StreamCallType callType, string callId)
            => Post<GoLiveResponseInternalDTO>($"/call/{callType}/{callId}/go_live");

        public Task<StopLiveResponseInternalDTO> StopLiveAsync(StreamCallType callType, string callId)
            => Post<StopLiveResponseInternalDTO>($"/call/{callType}/{callId}/stop_live");

        public Task<JoinCallResponseInternalDTO> JoinCallAsync(StreamCallType callType, string callId,
            JoinCallRequestInternalDTO joinCallRequest)
            => Post<JoinCallRequestInternalDTO, JoinCallResponseInternalDTO>($"/call/{callType}/{callId}/join", joinCallRequest);

        public Task<EndCallResponseInternalDTO> EndCallAsync(StreamCallType callType, string callId)
            => Post<EndCallResponseInternalDTO>($"/call/{callType}/{callId}/mark_ended");

        public Task<UpdateCallMembersResponseInternalDTO> UpdateCallMembersAsync(StreamCallType callType, string callId,
            UpdateCallMembersRequestInternalDTO updateCallMembersRequest)
            => Post<UpdateCallMembersRequestInternalDTO, UpdateCallMembersResponseInternalDTO>($"/call/{callType}/{callId}/members",
                updateCallMembersRequest);

        public Task<MuteUsersResponseInternalDTO> MuteUsersAsync(StreamCallType callType, string callId,
            MuteUsersRequestInternalDTO muteUsersRequest)
            => Post<MuteUsersRequestInternalDTO, MuteUsersResponseInternalDTO>($"/call/{callType}/{callId}/mute_users", muteUsersRequest);

        public Task<SendReactionResponseInternalDTO> SendVideoReactionAsync(StreamCallType callType, string callId,
            SendReactionRequestInternalDTO sendReactionRequest)
            => Post<SendReactionRequestInternalDTO, SendReactionResponseInternalDTO>($"/call/{callType}/{callId}/reaction",
                sendReactionRequest);

        public Task<RejectCallResponseInternalDTO> RejectCallAsync(StreamCallType callType, string callId)
            => Post<RejectCallResponseInternalDTO>($"/call/{callType}/{callId}/reject");

        public Task<RequestPermissionResponseInternalDTO> RequestPermissionAsync(StreamCallType callType, string callId,
            RequestPermissionRequestInternalDTO requestPermissionRequest)
            => Post<RequestPermissionRequestInternalDTO, RequestPermissionResponseInternalDTO>(
                $"/call/{callType}/{callId}/request_permission", requestPermissionRequest);

        public Task<UpdateUserPermissionsResponseInternalDTO> UpdateUserPermissionsAsync(StreamCallType callType, string callId,
            UpdateUserPermissionsRequestInternalDTO updateUserPermissionsRequest)
            => Post<UpdateUserPermissionsRequestInternalDTO, UpdateUserPermissionsResponseInternalDTO>(
                $"/call/{callType}/{callId}/user_permissions", updateUserPermissionsRequest);

        public Task<StartBroadcastingResponseInternalDTO> StartBroadcastingAsync(StreamCallType callType, string callId)
            => Post<StartBroadcastingResponseInternalDTO>($"/call/{callType}/{callId}/start_broadcasting");

        public Task<StopBroadcastingResponseInternalDTO> StopBroadcastingAsync(StreamCallType callType, string callId)
            => Post<StopBroadcastingResponseInternalDTO>($"/call/{callType}/{callId}/stop_broadcasting");

        public Task<StartRecordingResponseInternalDTO> StartRecordingAsync(StreamCallType callType, string callId)
            => Post<StartRecordingResponseInternalDTO>($"/call/{callType}/{callId}/start_recording");

        public Task<StopRecordingResponseInternalDTO> StopRecordingAsync(StreamCallType callType, string callId)
            => Post<StopRecordingResponseInternalDTO>($"/call/{callType}/{callId}/stop_recording");

        public Task<StartTranscriptionResponseInternalDTO> StartTranscriptionAsync(StreamCallType callType, string callId)
            => Post<StartTranscriptionResponseInternalDTO>($"/call/{callType}/{callId}/start_transcription");

        public Task<StopTranscriptionResponseInternalDTO> StopTranscriptionAsync(StreamCallType callType, string callId)
            => Post<StopTranscriptionResponseInternalDTO>($"/call/{callType}/{callId}/stop_transcription");

        public Task<QueryMembersResponseInternalDTO> QueryMembersAsync(QueryMembersRequestInternalDTO queryMembersRequest)
            => Post<QueryMembersRequestInternalDTO, QueryMembersResponseInternalDTO>($"/call/members", queryMembersRequest);

        public Task<QueryCallsResponseInternalDTO> QueryCallsAsync(StreamCallType callType, string callId,
            QueryCallsRequestInternalDTO queryCallsRequest)
            => Post<QueryCallsRequestInternalDTO, QueryCallsResponseInternalDTO>($"/calls", queryCallsRequest);

        public Task<ResponseInternalDTO> DeleteDeviceAsync(string deviceId, string userId)
        {
            var queryParams = QueryParameters.Default.Set(IdParamKey, deviceId).Set(UserIdParamKey, userId);
            return Delete<ResponseInternalDTO>($"/devices", queryParams);
        }

        public Task<ListDevicesResponseInternalDTO> ListDevicesAsync(string userId)
        {
            var queryParams = QueryParameters.Default.Set(UserIdParamKey, userId);
            return Get<ListDevicesResponseInternalDTO>($"/devices", queryParams);
        }

        public Task<ResponseInternalDTO> CreateDeviceAsync(CreateDeviceRequestInternalDTO createDeviceRequest)
            => Post<CreateDeviceRequestInternalDTO, ResponseInternalDTO>($"/devices", createDeviceRequest);
        
        public Task<GetEdgesResponseInternalDTO> GetEdgesAsync() => Get<GetEdgesResponseInternalDTO>($"/edges");
        
        public Task<CreateGuestResponseInternalDTO> CreateGuestAsync(CreateGuestRequestInternalDTO createGuestRequest)
            => Post<CreateGuestRequestInternalDTO, CreateGuestResponseInternalDTO>($"/guest", createGuestRequest);
        
        public Task<ResponseInternalDTO> VideoConnectAsync(WSAuthMessageRequestInternalDTO authMessageRequest)
            => Post<WSAuthMessageRequestInternalDTO, ResponseInternalDTO>($"/video/connect", authMessageRequest);

        private const string UserIdParamKey = "user_id";
        private const string IdParamKey = "id";
    }
}