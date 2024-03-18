using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.QueryBuilders.Sort.Calls
{
    /// <summary>
    /// Fields that you can use to sort <see cref="IStreamCall"/> query results when using <see cref="IStreamVideoClient.QueryCallsAsync"/>
    /// </summary>
    public readonly struct CallSortField
    {
        public static CallSortField Type => new CallSortField("type");
        public static CallSortField Id => new CallSortField("id");
        public static CallSortField Cid => new CallSortField("cid");
        public static CallSortField StartsAt => new CallSortField("starts_at");
        public static CallSortField CreatedAt => new CallSortField("created_at");
        public static CallSortField UpdatedAt => new CallSortField("updated_at");

        internal readonly string FieldName;

        public CallSortField(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}