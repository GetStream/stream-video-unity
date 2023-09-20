using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.Models;

namespace StreamVideo.Core.QueryBuilders.Filters.CallMembers
{
    /// <summary>
    /// Filter by <see cref="CallMember.UserId"/>
    /// </summary>
    public sealed class CallMemberFieldId : BaseFieldToFilter
    {
        public override string FieldName => "id";

        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is EQUAL to provided member Id
        /// </summary>
        public FieldFilterRule EqualsTo(string userId) => InternalEqualsTo(userId);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is EQUAL to provided member
        /// </summary>
        public FieldFilterRule EqualsTo(Models.CallMember member) => InternalEqualsTo(member.UserId);

        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is EQUAL to ANY of provided member Id
        /// </summary>
        public FieldFilterRule In(IEnumerable<string> userIds) => InternalIn(userIds);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is EQUAL to ANY of provided member Id
        /// </summary>
        public FieldFilterRule In(params string[] userIds) => InternalIn(userIds);

        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is EQUAL to ANY of the provided member Id
        /// </summary>
        public FieldFilterRule In(IEnumerable<CallMember> members)
            => InternalIn(members.Select(_ => _.UserId));

        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is EQUAL to ANY of the provided member Id
        /// </summary>
        public FieldFilterRule In(params CallMember[] members)
            => InternalIn(members.Select(_ => _.UserId));
        
        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is GREATER THAN the provided one
        /// </summary>
        public FieldFilterRule GreaterThan(string userId) => InternalGreaterThan(userId);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is GREATER THAN the provided one
        /// </summary>
        public FieldFilterRule GreaterThan(CallMember member) => InternalGreaterThan(member.UserId);

        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is GREATER THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule GreaterThanOrEquals(string userId) => InternalGreaterThanOrEquals(userId);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is GREATER THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule GreaterThanOrEquals(CallMember member) => InternalGreaterThanOrEquals(member.UserId);

        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is LESS THAN the provided one
        /// </summary>
        public FieldFilterRule LessThan(string userId) => InternalLessThan(userId);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is LESS THAN the provided one
        /// </summary>
        public FieldFilterRule LessThan(CallMember member) => InternalLessThan(member.UserId);

        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is LESS THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule LessThanOrEquals(string userId) => InternalLessThanOrEquals(userId);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.UserId"/> is LESS THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule LessThanOrEquals(CallMember member) => InternalLessThanOrEquals(member.UserId);
    }
}