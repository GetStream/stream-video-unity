using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall.Type"/>
    /// </summary>
    public sealed class CallFieldType : BaseFieldToFilter
    {
        public override string FieldName => "type";

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Type"/> state is EQUAL to the provided value
        /// </summary>
        public FieldFilterRule EqualsTo(StreamCallType callType)
            => InternalEqualsTo(callType.ToString());

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Type"/> is EQUAL to ANY of provided values
        /// </summary>
        public FieldFilterRule In(IEnumerable<StreamCallType> callTypes)
            => InternalIn(callTypes.Select(_ => _.ToString()));
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Type"/> is EQUAL to ANY of provided values
        /// </summary>
        public FieldFilterRule In(params StreamCallType[] callTypes)
            => InternalIn(callTypes.Select(_ => _.ToString()));
    }
}