using StreamVideo.Core.Models;

namespace StreamVideo.Core.QueryBuilders.Sort
{
    /// <summary>
    /// Fields that you can use to sort <see cref="IStreamUser"/> query results when using <see cref="IStreamChatClient.QueryUsersAsync"/>
    /// </summary>
    public readonly struct CallMemberSortField
    {
        public static CallMemberSortField Name => new CallMemberSortField("name");
        public static CallMemberSortField Role => new CallMemberSortField("role");
        public static CallMemberSortField Banned => new CallMemberSortField("banned");
        public static CallMemberSortField ShadowBanned => new CallMemberSortField("shadow_banned");
        public static CallMemberSortField CreatedAt => new CallMemberSortField("created_at");
        public static CallMemberSortField UpdatedAt => new CallMemberSortField("updated_at");
        public static CallMemberSortField LastActive => new CallMemberSortField("last_active");
        public static CallMemberSortField Teams => new CallMemberSortField("teams");

        /// <summary>
        /// Sort by your custom field of <see cref="CallMember"/>
        /// </summary>
        public static CallMemberSortField CustomField(string fieldName) => new CallMemberSortField(fieldName);

        internal readonly string FieldName;

        public CallMemberSortField(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}