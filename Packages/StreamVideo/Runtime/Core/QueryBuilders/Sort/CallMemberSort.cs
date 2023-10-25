using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.QueryBuilders.Sort
{
    /// <summary>
    /// Sort object for <see cref="IStreamVideoUser"/> query: <see cref="IStreamCall.QueryMembersAsync"/>
    /// </summary>
    public sealed class CallMemberSort : QuerySortBase<CallMemberSort, CallMemberSortField>
    {
        /// <summary>
        /// Sort in ascending order meaning from lowest to highest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallMemberSort OrderByAscending(CallMemberSortField fieldName)
        {
            var instance = new CallMemberSort();
            instance.InternalOrderByAscending(fieldName);
            return instance;
        }

        /// <summary>
        /// Sort in descending order meaning from highest to lowest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallMemberSort OrderByDescending(CallMemberSortField fieldName)
        {
            var instance = new CallMemberSort();
            instance.InternalOrderByDescending(fieldName);
            return instance;
        }
        
        protected override CallMemberSort Instance => this;

        protected override string ToUnderlyingFieldName(CallMemberSortField field) => field.FieldName;
    }
}