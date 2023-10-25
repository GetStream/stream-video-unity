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

        private IStreamVideoClient _client;
    }
}