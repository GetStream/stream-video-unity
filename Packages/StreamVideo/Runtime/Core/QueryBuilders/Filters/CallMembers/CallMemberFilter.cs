using StreamVideo.Core.Models;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.QueryBuilders.Filters.CallMembers
{
    /// <summary>
    /// Filters for <see cref="CallMember"/> query filters in <see cref="IStreamCall.QueryMembersAsync"/>
    /// </summary>
    public static class CallMemberFilter
    {
        /// <inheritdoc cref="CallMemberFieldId"/>
        public static CallMemberFieldId Id { get; } = new CallMemberFieldId();

        /// <inheritdoc cref="CallMemberFieldRole"/>
        public static CallMemberFieldRole Role { get; } = new CallMemberFieldRole();

        /// <inheritdoc cref="CallMemberFieldCreatedAt"/>
        public static CallMemberFieldCreatedAt CreatedAt { get; } = new CallMemberFieldCreatedAt();

        /// <inheritdoc cref="CallMemberFieldUpdatedAt"/>
        public static CallMemberFieldUpdatedAt UpdatedAt { get; } = new CallMemberFieldUpdatedAt();

        /// <inheritdoc cref="CallMemberFieldCustom"/>
        public static CallMemberFieldCustom Custom(string customFieldName) => new CallMemberFieldCustom(customFieldName);
    }
}