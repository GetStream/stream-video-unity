using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Sort.Calls
{
    /// <summary>
    /// Extensions for <see cref="IStreamCall"/> query <see cref="IStreamVideoClient.QueryCallsAsync"/> sort object building
    /// </summary>
    public static class CallSortExt
    {
        /// <summary>
        /// Sort in descending order meaning from highest to lowest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallSort ThenByAscending(this CallSort sort, CallSortField fieldName)
            => sort.InternalOrderByAscending(fieldName);

        /// <summary>
        /// Sort in descending order meaning from highest to lowest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallSort ThenByDescending(this CallSort sort, CallSortField fieldName)
            => sort.InternalOrderByDescending(fieldName);
    }
}