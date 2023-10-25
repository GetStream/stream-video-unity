using System.Collections.Generic;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall.Cid"/>
    /// </summary>
    public sealed class CallFieldCid : BaseFieldToFilter
    {
        public override string FieldName => "cid";

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Cid"/> is EQUAL to provided call Cid
        /// </summary>
        public FieldFilterRule EqualsTo(string callCid) => InternalEqualsTo(callCid);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Cid"/> is EQUAL to ANY of provided call Cid
        /// </summary>
        public FieldFilterRule In(IEnumerable<string> callCids) => InternalIn(callCids);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Cid"/> is EQUAL to ANY of provided call Cid
        /// </summary>
        public FieldFilterRule In(params string[] callCids) => InternalIn(callCids);
    }
}