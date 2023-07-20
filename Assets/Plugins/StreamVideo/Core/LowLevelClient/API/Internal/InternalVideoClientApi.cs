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
    internal class InternalVideoClientApi : InternalApiClientBase, IInternalVideoClientApi
    {
        public InternalVideoClientApi(IHttpClient httpClient, ISerializer serializer, ILogs logs,
            IRequestUriFactory requestUriFactory, IStreamVideoLowLevelClient lowLevelClient) : base(httpClient,
            serializer, logs, requestUriFactory, lowLevelClient)
        {
        }

        public Task<GetCallResponse> GetCallAsync(GetOrCreateCallRequest getCallRequest)
            => Get<GetOrCreateCallRequest, GetCallResponse>("/users", getCallRequest);

        public Task<UpdateCallResponse> UpdateCallAsync(StreamCallType callType, string callId,
            UpdateCallRequest updateCallRequest)
            => Patch<UpdateCallRequest, UpdateCallResponse>($"/call/{callType}/{callId}", updateCallRequest);

        public Task<GetOrCreateCallResponse> GetOrCreateCallAsync(StreamCallType callType, string callId,
            GetOrCreateCallRequest getOrCreateCallRequest)
            => Post<GetOrCreateCallRequest, GetOrCreateCallResponse>($"/call/{callType}/{callId}",
                getOrCreateCallRequest);

        public Task<AcceptCallResponse> AcceptCallAsync(StreamCallType callType, string callId)
            => Post<AcceptCallResponse>($"/call/{callType}/{callId}/accept");

        public Task<BlockUserResponse> BlockUserAsync(StreamCallType callType, string callId,
            BlockUserRequest blockUserRequest)
            => Post<BlockUserRequest, BlockUserResponse>($"/call/{callType}/{callId}/block", blockUserRequest);

        public Task<UnblockUserResponse> UnblockUserAsync(StreamCallType callType, string callId,
            UnblockUserRequest unblockUserRequest)
            => Post<UnblockUserRequest, UnblockUserResponse>($"/call/{callType}/{callId}/unblock", unblockUserRequest);

        public Task<SendEventResponse> SendEventAsync(StreamCallType callType, string callId,
            SendEventRequest sendEventRequest)
            => Post<SendEventRequest, SendEventResponse>($"/call/{callType}/{callId}/event", sendEventRequest);

        public Task<GoLiveResponse> GoLiveAsync(StreamCallType callType, string callId)
            => Post<GoLiveResponse>($"/call/{callType}/{callId}/go_live");

        public Task<StopLiveResponse> StopLiveAsync(StreamCallType callType, string callId)
            => Post<StopLiveResponse>($"/call/{callType}/{callId}/stop_live");

        public Task<JoinCallResponse> JoinCallAsync(StreamCallType callType, string callId,
            JoinCallRequest joinCallRequest)
            => Post<JoinCallRequest, JoinCallResponse>($"/call/{callType}/{callId}/join", joinCallRequest);

        public Task<EndCallResponse> EndCallAsync(StreamCallType callType, string callId)
            => Post<EndCallResponse>($"/call/{callType}/{callId}/mark_ended");

        public Task<UpdateCallMembersResponse> UpdateCallMembersAsync(StreamCallType callType, string callId,
            UpdateCallMembersRequest updateCallMembersRequest)
            => Post<UpdateCallMembersRequest, UpdateCallMembersResponse>($"/call/{callType}/{callId}/members",
                updateCallMembersRequest);

        public Task<MuteUsersResponse> MuteUsersAsync(StreamCallType callType, string callId,
            MuteUsersRequest muteUsersRequest)
            => Post<MuteUsersRequest, MuteUsersResponse>($"/call/{callType}/{callId}/mute_users", muteUsersRequest);

        public Task<SendReactionResponse> SendVideoReactionAsync(StreamCallType callType, string callId,
            SendReactionRequest sendReactionRequest)
            => Post<SendReactionRequest, SendReactionResponse>($"/call/{callType}/{callId}/reaction",
                sendReactionRequest);

        public Task<RejectCallResponse> RejectCallAsync(StreamCallType callType, string callId)
            => Post<RejectCallResponse>($"/call/{callType}/{callId}/reject");

        public Task<RequestPermissionResponse> RequestPermissionAsync(StreamCallType callType, string callId,
            RequestPermissionRequest requestPermissionRequest)
            => Post<RequestPermissionRequest, RequestPermissionResponse>(
                $"/call/{callType}/{callId}/request_permission", requestPermissionRequest);

        public Task<UpdateUserPermissionsResponse> UpdateUserPermissionsAsync(StreamCallType callType, string callId,
            UpdateUserPermissionsRequest updateUserPermissionsRequest)
            => Post<UpdateUserPermissionsRequest, UpdateUserPermissionsResponse>(
                $"/call/{callType}/{callId}/user_permissions", updateUserPermissionsRequest);

        public Task<StartBroadcastingResponse> StartBroadcastingAsync(StreamCallType callType, string callId)
            => Post<StartBroadcastingResponse>($"/call/{callType}/{callId}/start_broadcasting");

        public Task<StopBroadcastingResponse> StopBroadcastingAsync(StreamCallType callType, string callId)
            => Post<StopBroadcastingResponse>($"/call/{callType}/{callId}/stop_broadcasting");

        public Task<StartRecordingResponse> StartRecordingAsync(StreamCallType callType, string callId)
            => Post<StartRecordingResponse>($"/call/{callType}/{callId}/start_recording");

        public Task<StopRecordingResponse> StopRecordingAsync(StreamCallType callType, string callId)
            => Post<StopRecordingResponse>($"/call/{callType}/{callId}/stop_recording");

        public Task<StartTranscriptionResponse> StartTranscriptionAsync(StreamCallType callType, string callId)
            => Post<StartTranscriptionResponse>($"/call/{callType}/{callId}/start_transcription");

        public Task<StopTranscriptionResponse> StopTranscriptionAsync(StreamCallType callType, string callId)
            => Post<StopTranscriptionResponse>($"/call/{callType}/{callId}/stop_transcription");

        public Task<QueryMembersResponse> QueryMembersAsync(QueryMembersRequest queryMembersRequest)
            => Post<QueryMembersRequest, QueryMembersResponse>($"/call/members", queryMembersRequest);

        public Task<QueryCallsResponse> QueryCallsAsync(StreamCallType callType, string callId,
            QueryCallsRequest queryCallsRequest)
            => Post<QueryCallsRequest, QueryCallsResponse>($"/calls", queryCallsRequest);

        public Task<Response> DeleteDeviceAsync(string deviceId, string userId)
        {
            var queryParams = QueryParameters.Default.Set(IdParamKey, deviceId).Set(UserIdParamKey, userId);
            return Delete<Response>($"/devices", queryParams);
        }

        public Task<ListDevicesResponse> ListDevicesAsync(string userId)
        {
            var queryParams = QueryParameters.Default.Set(UserIdParamKey, userId);
            return Get<ListDevicesResponse>($"/devices", queryParams);
        }

        public Task<Response> CreateDeviceAsync(CreateDeviceRequest createDeviceRequest)
            => Post<CreateDeviceRequest, Response>($"/devices", createDeviceRequest);
        
        public Task<GetEdgesResponse> GetEdgesAsync() => Get<GetEdgesResponse>($"/edges");
        
        public Task<CreateGuestResponse> CreateGuestAsync(CreateGuestRequest createGuestRequest)
            => Post<CreateGuestRequest, CreateGuestResponse>($"/guest", createGuestRequest);
        
        public Task<Response> VideoConnectAsync(WSAuthMessageRequest authMessageRequest)
            => Post<WSAuthMessageRequest, Response>($"/video/connect", authMessageRequest);

        private const string UserIdParamKey = "user_id";
        private const string IdParamKey = "id";
    }
}