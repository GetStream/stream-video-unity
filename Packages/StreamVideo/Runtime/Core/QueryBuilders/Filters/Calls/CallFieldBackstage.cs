using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall.Backstage"/>
    /// </summary>
    public sealed class CallFieldBackstage : BaseFieldToFilter
    {
        public override string FieldName => "backstage";

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Backstage"/> state is EQUAL to the provided value
        /// </summary>
        public FieldFilterRule EqualsTo(bool isBackstage) => InternalEqualsTo(isBackstage);
    }
}