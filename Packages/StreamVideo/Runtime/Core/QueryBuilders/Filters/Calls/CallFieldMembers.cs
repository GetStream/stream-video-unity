using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.Models;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall.Members"/>
    /// </summary>
    public sealed class CallFieldMembers : BaseFieldToFilter
    {
        public override string FieldName => "members";

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contains a user with provided user ID
        /// </summary>
        public FieldFilterRule EqualsTo(string userId) => InternalEqualsTo(userId);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contains a provided user
        /// </summary>
        public FieldFilterRule EqualsTo(IStreamVideoUser user) => InternalEqualsTo(user.Id);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(IEnumerable<string> userIds) => InternalIn(userIds);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(params string[] userIds) => InternalIn(userIds);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(IEnumerable<IStreamVideoUser> userIds)
            => InternalIn(userIds.Select(_ => _.Id));
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(params IStreamVideoUser[] userIds)
            => InternalIn(userIds.Select(_ => _.Id));

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(IEnumerable<IStreamVideoCallParticipant> userIds)
            => InternalIn(userIds.Select(_ => _.User.Id));
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(params IStreamVideoCallParticipant[] userIds)
            => InternalIn(userIds.Select(_ => _.User.Id));
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(IEnumerable<CallMember> userIds)
            => InternalIn(userIds.Select(_ => _.User.Id));
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.Members"/> contain any of the users with provided user IDs
        /// </summary>
        public FieldFilterRule In(params CallMember[] userIds)
            => InternalIn(userIds.Select(_ => _.User.Id));
    }
}