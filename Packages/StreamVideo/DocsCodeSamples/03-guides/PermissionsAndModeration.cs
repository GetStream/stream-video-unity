using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.Models;
using StreamVideo.Core.StatefulModels;

namespace StreamVideoDocsCodeSamples._03_guides
{
    /// <summary>
    /// Code examples for guides/permissions-and-moderation/ page
    /// </summary>
    internal class PermissionsAndModeration
    {
        public async Task CheckPermission()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            var canSendAudio = streamCall.HasPermissions(OwnCapability.SendAudio);
        }

        public async Task RequestPermission()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            // Send permission request
            await streamCall.RequestPermissionAsync(OwnCapability.SendAudio);
        }

        public async Task GrantPermission()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            IStreamVideoUser user = null;
            IStreamVideoCallParticipant participant = null;

            // Grant permission to user with specific user ID
            await streamCall.GrantPermissionsAsync(new[] { OwnCapability.SendAudio }, "user-id");

            // Grant permission to user using instance of IStreamVideoUser
            await streamCall.GrantPermissionsAsync(new[] { OwnCapability.SendAudio }, user);

            // Grant permission to user using instance of IStreamVideoCallParticipant
            await streamCall.GrantPermissionsAsync(new[] { OwnCapability.SendAudio }, participant);
        }

        public Task GrantRequestedPermissions()
        {
            //StreamTodo: implement streamCall.PermissionRequests
            /*
             *val requests = call.state.permissionRequests.value
                requests.forEach {
                    it.grant() // or it.reject()
                }
             * 
             */
            return Task.CompletedTask;
        }

        public async Task BlockUser()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            IStreamVideoUser user = null;
            IStreamVideoCallParticipant participant = null;

            // Block user in a call using their user ID
            await streamCall.BlockUserAsync("user-id");

            // Block user in a call using their instance of IStreamVideoUser
            await streamCall.BlockUserAsync(user);

            // Block user in a call using their instance of IStreamVideoCallParticipant
            await streamCall.BlockUserAsync(participant);
        }

        public async Task RemoveUser()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            IStreamVideoUser user = null;
            IStreamVideoCallParticipant participant = null;

            // Remove user from a call using their user ID
            await streamCall.RemoveMembersAsync(new[] { "user-id" });

            // Remove user from a call using their instance of IStreamVideoUser
            await streamCall.RemoveMembersAsync(new[] { user });

            // Remove user from a call using their instance of IStreamVideoCallParticipant
            await streamCall.RemoveMembersAsync(new[] { participant });
        }

        public async Task MuteUsers()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            IStreamVideoUser user = null;
            IStreamVideoCallParticipant participant = null;

            // Mute user in a call using their user ID and choose which of their tracks you want to mute: audio, video, or screenShare
            await streamCall.MuteUsersAsync(new[] { "user-id" }, audio: true, video: true, screenShare: true);

            // Mute user in a call using their instance of IStreamVideoUser and choose which of their tracks you want to mute: audio, video, or screenShare
            await streamCall.MuteUsersAsync(new[] { user }, audio: true, video: true, screenShare: true);

            // Mute user in a call using their instance of IStreamVideoCallParticipant and choose which of their tracks you want to mute: audio, video, or screenShare
            await streamCall.MuteUsersAsync(new[] { participant }, audio: true, video: true, screenShare: true);
        }

        public async Task MuteAllUsers()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            // Mute all user in a call and choose which of their tracks you want to mute: audio, video, or screenShare
            await streamCall.MuteAllUsersAsync(audio: true, video: true, screenShare: true);
        }

        private IStreamVideoClient _client;
    }
}