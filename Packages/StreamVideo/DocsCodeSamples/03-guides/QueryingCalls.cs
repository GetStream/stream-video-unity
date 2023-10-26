using System;
using System.Collections.Generic;
using Core.QueryBuilders.Filters.Calls;
using Core.QueryBuilders.Sort.Calls;
using StreamVideo.Core;
using StreamVideo.Core.QueryBuilders.Filters;

namespace DocsCodeSamples._03_guides
{
    /// <summary>
    /// Code examples for guides/querying-calls/ page
    /// </summary>
    internal class QueryingCalls
    {
        public async void QueryCalls()
        {
            var filters = new List<IFieldFilterRule>
            {
                CallFilter.CreatedAt.GreaterThan(DateTime.Now.AddHours(-24))
            };

            var result = await _client.QueryCallsAsync(filters, CallSort.OrderByDescending(CallSortField.CreatedAt), limit: 25);

            // queried calls, depending on how many calls satisfy the filter this can be only a subset or a single "page" of results
            var members = result.Calls;

            // In order to get the next "page" of results, use this token as a "next" argument in the QueryCallsAsync method
            var next = result.Next;

            // In order to get the previous "page" of results, use this token as a "prev" argument in the QueryCallsAsync method
            var prev = result.Prev;
        }
        
        public async void QueryCallsThatWillStartSoon()
        {
            // Filter calls that will start in 3 hours and include me
            var filters = new List<IFieldFilterRule>
            {
                CallFilter.StartsAt.LessThanOrEquals(DateTime.Now.AddHours(3)),
                CallFilter.Members.EqualsTo(_client.LocalUser)
            };

            // Order them by how soon they start
            var result = await _client.QueryCallsAsync(filters, CallSort.OrderByAscending(CallSortField.StartsAt), limit: 25);
        }
        
        public async void QueryCallsByCustom()
        {
            //StreamTodo: add option to set custom data on IStreamCall
 
            // Filter calls by custom property. For example you can attach a "tag" property to a call and get calls that contain any of the provided tags 
            var filters = new List<IFieldFilterRule>
            {
                CallFilter.Custom("tag").In("xbox", "ps", "switch"),
            };

            // Order them by how soon they start
            var sort = CallSort.OrderByAscending(CallSortField.StartsAt);

            var result = await _client.QueryCallsAsync(filters, sort, limit: 25);
        }

        private IStreamVideoClient _client;
    }
}