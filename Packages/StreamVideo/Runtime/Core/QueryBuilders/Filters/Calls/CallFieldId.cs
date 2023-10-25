using System.Collections.Generic;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall.Id"/>
    /// </summary>
    public sealed class CallFieldId : BaseFieldToFilter
    {
        public override string FieldName => "id";

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Id"/> is EQUAL to provided call Id
        /// </summary>
        public FieldFilterRule EqualsTo(string callId) => InternalEqualsTo(callId);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Id"/> is EQUAL to ANY of provided call Id
        /// </summary>
        public FieldFilterRule In(IEnumerable<string> callIds) => InternalIn(callIds);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Id"/> is EQUAL to ANY of provided call Id
        /// </summary>
        public FieldFilterRule In(params string[] callIds) => InternalIn(callIds);
    }
}