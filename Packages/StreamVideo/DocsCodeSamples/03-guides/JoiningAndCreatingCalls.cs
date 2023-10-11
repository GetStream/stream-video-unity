using System.Collections.Generic;
using StreamVideo.Core;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.QueryBuilders.Filters.CallMembers;
using StreamVideo.Core.QueryBuilders.Sort;

namespace DocsCodeSamples._03_guides
{
    internal class JoiningAndCreatingCalls
    {
        public async void GetCall()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Try to get call - will return null if the call doesn't exist
            var streamCall = await _client.GetCallAsync(callType, callId);
        }

        public async void GetOrCreateCall()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Get call or create if it doesn't exist
            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);
        }

        public async void CreateCallAndJoin()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Notice that we pass create argument as true - this will create the call if it doesn't already exist
            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);
        }

        public async void JoinOtherCall()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Notice that we pass create argument as false - if the call doesn't exist the join attempt will fail
            var streamCall = await _client.JoinCallAsync(callType, callId, create: false, ring: true, notify: false);
        }

        public async void QueryMembers()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Notice that we pass create argument as false - if the call doesn't exist the join attempt will fail
            var streamCall = await _client.JoinCallAsync(callType, callId, create: false, ring: true, notify: false);

            var filters = new List<IFieldFilterRule>
            {
                CallMemberFilter.Role.EqualsTo("admin")
            };
            var result = await streamCall.QueryMembersAsync(filters, CallMemberSort.OrderByDescending(CallMemberSortField.LastActive), limit: 25);

            // queried members, depending on how many members satisfy the filter this can be only a subset or a single "page" of results
            var members = result.Members;

            // In order to get the next "page" of results, use this token as a "next" argument in the QueryMembersAsync method
            var next = result.Next;

            // In order to get the previous "page" of results, use this token as a "prev" argument in the QueryMembersAsync method
            var prev = result.Prev;
        }
        
        private IStreamVideoClient _client;
    }
}