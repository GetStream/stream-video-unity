using StreamVideo.Core;
using StreamVideo.Core.QueryBuilders.Sort;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Sort.Calls
{
    /// <summary>
    /// Sort object for <see cref="IStreamCall"/> query: <see cref="IStreamVideoClient.QueryCallsAsync"/>
    /// </summary>
    public sealed class CallSort : QuerySortBase<CallSort, CallSortField>
    {
        /// <summary>
        /// Sort in ascending order meaning from lowest to highest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallSort OrderByAscending(CallSortField fieldName)
        {
            var instance = new CallSort();
            instance.InternalOrderByAscending(fieldName);
            return instance;
        }

        /// <summary>
        /// Sort in descending order meaning from highest to lowest value of the specified field
        /// </summary>
        /// <param name="fieldName">Field name to sort by</param>
        public static CallSort OrderByDescending(CallSortField fieldName)
        {
            var instance = new CallSort();
            instance.InternalOrderByDescending(fieldName);
            return instance;
        }
        
        protected override CallSort Instance => this;

        protected override string ToUnderlyingFieldName(CallSortField field) => field.FieldName;
    }
}