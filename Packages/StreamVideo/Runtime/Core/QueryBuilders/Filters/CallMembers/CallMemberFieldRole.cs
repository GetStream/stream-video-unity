using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.Models;

namespace StreamVideo.Core.QueryBuilders.Filters.CallMembers
{
    /// <summary>
    /// Filter by <see cref="CallMember.Role"/>
    /// </summary>
    public sealed class CallMemberFieldRole : BaseFieldToFilter
    {
        public override string FieldName => "role";

        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is EQUAL to provided member Id
        /// </summary>
        public FieldFilterRule EqualsTo(string memberRole) => InternalEqualsTo(memberRole);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is EQUAL to provided member
        /// </summary>
        public FieldFilterRule EqualsTo(CallMember member) => InternalEqualsTo(member.Role);

        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is EQUAL to ANY of provided member Id
        /// </summary>
        public FieldFilterRule In(IEnumerable<string> memberRoles) => InternalIn(memberRoles);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is EQUAL to ANY of provided member Id
        /// </summary>
        public FieldFilterRule In(params string[] memberRoles) => InternalIn(memberRoles);

        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is EQUAL to ANY of the provided member Id
        /// </summary>
        public FieldFilterRule In(IEnumerable<CallMember> members)
            => InternalIn(members.Select(_ => _.Role));

        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is EQUAL to ANY of the provided member Id
        /// </summary>
        public FieldFilterRule In(params CallMember[] members)
            => InternalIn(members.Select(_ => _.Role));
        
        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is GREATER THAN the provided one
        /// </summary>
        public FieldFilterRule GreaterThan(string memberRole) => InternalGreaterThan(memberRole);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is GREATER THAN the provided one
        /// </summary>
        public FieldFilterRule GreaterThan(CallMember member) => InternalGreaterThan(member.Role);

        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is GREATER THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule GreaterThanOrEquals(string memberRole) => InternalGreaterThanOrEquals(memberRole);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is GREATER THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule GreaterThanOrEquals(CallMember member) => InternalGreaterThanOrEquals(member.Role);

        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is LESS THAN the provided one
        /// </summary>
        public FieldFilterRule LessThan(string memberRole) => InternalLessThan(memberRole);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is LESS THAN the provided one
        /// </summary>
        public FieldFilterRule LessThan(CallMember member) => InternalLessThan(member.Role);

        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is LESS THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule LessThanOrEquals(string memberRole) => InternalLessThanOrEquals(memberRole);
        
        /// <summary>
        /// Return only members where <see cref="CallMember.Role"/> is LESS THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule LessThanOrEquals(CallMember member) => InternalLessThanOrEquals(member.Role);
    }
}