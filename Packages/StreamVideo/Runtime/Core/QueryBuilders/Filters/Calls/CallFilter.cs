using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filters for <see cref="IStreamCall"/> query filters in <see cref="IStreamVideoClient.QueryCallsAsync"/>
    /// </summary>
    public static class CallFilter
    {
        /// <inheritdoc cref="CallFieldType"/>
        public static CallFieldType Type { get; } = new CallFieldType();

        /// <inheritdoc cref="CallFieldId"/>
        public static CallFieldId Id { get; } = new CallFieldId();

        /// <inheritdoc cref="CallFieldCid"/>
        public static CallFieldCid Cid { get; } = new CallFieldCid();

        /// <inheritdoc cref="CallFieldCreatedByUserId"/>
        public static CallFieldCreatedByUserId CreatedByUserId { get; } = new CallFieldCreatedByUserId();
        
        /// <inheritdoc cref="CallFieldCreatedAt"/>
        public static CallFieldCreatedAt CreatedAt { get; } = new CallFieldCreatedAt();
        
        /// <inheritdoc cref="CallFieldUpdatedAt"/>
        public static CallFieldUpdatedAt UpdatedAt { get; } = new CallFieldUpdatedAt();
        
        /// <inheritdoc cref="CallFieldStartsAt"/>
        public static CallFieldStartsAt StartsAt { get; } = new CallFieldStartsAt();
        
        /// <inheritdoc cref="CallFieldEndedAt"/>
        public static CallFieldEndedAt EndedAt { get; } = new CallFieldEndedAt();
        
        /// <inheritdoc cref="CallFieldBackstage"/>
        public static CallFieldBackstage Backstage { get; } = new CallFieldBackstage();
        
        /// <inheritdoc cref="CallFieldMembers"/>
        public static CallFieldMembers Members { get; } = new CallFieldMembers();

        /// <inheritdoc cref="CallFieldCustom"/>
        public static CallFieldCustom Custom(string customFieldName) => new CallFieldCustom(customFieldName);
    }
}