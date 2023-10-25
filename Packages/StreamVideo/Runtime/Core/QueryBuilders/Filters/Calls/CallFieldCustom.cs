using System.Collections.Generic;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall"/> custom field.
    /// </summary>
    public sealed class CallFieldCustom : BaseFieldToFilter
    {
        public override string FieldName { get; }

        public CallFieldCustom(string customFieldName)
        {
            //StreamAsserts.AssertNotNullOrEmpty(customFieldName, nameof(customFieldName));
            FieldName = customFieldName;
        }

        /// <summary>
        /// Return only calls where <see cref="IStreamCall"/> custom field has value EQUAL to the provided one
        /// </summary>
        public FieldFilterRule EqualsTo(string value) => InternalEqualsTo(value);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall"/> custom field has value EQUAL to ANY of provided channel Id.
        /// </summary>
        public FieldFilterRule In(IEnumerable<string> values) => InternalIn(values);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall"/> custom field has value EQUAL to ANY of provided channel Id.
        /// </summary>
        public FieldFilterRule In(params string[] values) => InternalIn(values);
    }
}