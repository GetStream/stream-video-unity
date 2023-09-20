using System.Collections.Generic;
using StreamVideo.Core.Models;

namespace StreamVideo.Core.QueryBuilders.Filters.CallMembers
{
    /// <summary>
    /// Filter by <see cref="CallMember"/> custom field.
    /// </summary>
    public sealed class CallMemberFieldCustom : BaseFieldToFilter
    {
        public override string FieldName { get; }

        public CallMemberFieldCustom(string customFieldName)
        {
            //StreamAsserts.AssertNotNullOrEmpty(customFieldName, nameof(customFieldName));
            FieldName = customFieldName;
        }

        /// <summary>
        /// Return only channels where <see cref="IStreamChannel"/> custom field has value EQUAL to the provided one
        /// </summary>
        public FieldFilterRule EqualsTo(string value) => InternalEqualsTo(value);
        
        /// <summary>
        /// Return only channels where <see cref="IStreamChannel"/> custom field has value EQUAL to ANY of provided channel Id.
        /// </summary>
        public FieldFilterRule In(IEnumerable<string> values) => InternalIn(values);
        
        /// <summary>
        /// Return only channels where <see cref="IStreamChannel"/> custom field has value EQUAL to ANY of provided channel Id.
        /// </summary>
        public FieldFilterRule In(params string[] values) => InternalIn(values);
    }
}