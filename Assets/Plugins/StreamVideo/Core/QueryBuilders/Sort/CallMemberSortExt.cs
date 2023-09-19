using StreamVideo.Core.Models;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.QueryBuilders.Sort
{
    /// <summary>
    /// Extensions for <see cref="CallMember"/> query <see cref="IStreamCall.QueryMembersAsync"/> sort object building
    /// </summary>
    public static class CallMemberSortExt
    {
        /// <summary>
        /// Sort in descending order meaning from highest to lowest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallMemberSort ThenByAscending(this CallMemberSort sort, CallMemberSortField fieldName)
            => sort.InternalOrderByAscending(fieldName);

        /// <summary>
        /// Sort in descending order meaning from highest to lowest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallMemberSort ThenByDescending(this CallMemberSort sort, CallMemberSortField fieldName)
            => sort.InternalOrderByDescending(fieldName);
    }
}