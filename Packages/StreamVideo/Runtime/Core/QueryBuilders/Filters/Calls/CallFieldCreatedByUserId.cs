using StreamVideo.Core.Models;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall.CreatedBy"/> of a user who created the <see cref="IStreamCall"/>
    /// </summary>
    public sealed class CallFieldCreatedByUserId : BaseFieldToFilter
    {
        public override string FieldName => "created_by_user_id";
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.CreatedBy"/> is EQUAL to the provided user ID
        /// </summary>
        public FieldFilterRule EqualsTo(string userId) => InternalEqualsTo(userId);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.CreatedBy"/> is EQUAL to the provided user
        /// </summary>
        public FieldFilterRule EqualsTo(IStreamVideoUser user) => InternalEqualsTo(user.Id);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.CreatedBy"/> is EQUAL to the local user
        /// </summary>
        public FieldFilterRule EqualsTo(IStreamVideoCallParticipant participant) => InternalEqualsTo(participant.UserId);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.CreatedBy"/> is EQUAL to the local user
        /// </summary>
        public FieldFilterRule EqualsTo(CallMember member) => InternalEqualsTo(member.UserId);
    }
}