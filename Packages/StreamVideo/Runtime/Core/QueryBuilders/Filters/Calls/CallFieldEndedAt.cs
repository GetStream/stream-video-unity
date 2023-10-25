using System;
using StreamVideo.Core.QueryBuilders.Filters;
using StreamVideo.Core.StatefulModels;

namespace Core.QueryBuilders.Filters.Calls
{
    /// <summary>
    /// Filter by <see cref="IStreamCall.EndedAt"/>
    /// </summary>
    public sealed class CallFieldEndedAt : BaseFieldToFilter
    {
        public override string FieldName => "ended_at";

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is EQUAL to the provided one
        /// </summary>
        public FieldFilterRule EqualsTo(DateTime createdAt) => InternalEqualsTo(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is EQUAL to the provided one
        /// </summary>
        public FieldFilterRule EqualsTo(DateTimeOffset createdAt) => InternalEqualsTo(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is GREATER THAN the provided one
        /// </summary>
        public FieldFilterRule GreaterThan(DateTime createdAt) => InternalGreaterThan(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is GREATER THAN the provided one
        /// </summary>
        public FieldFilterRule GreaterThan(DateTimeOffset createdAt) => InternalGreaterThan(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is GREATER THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule GreaterThanOrEquals(DateTime createdAt)
            => InternalGreaterThanOrEquals(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is GREATER THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule GreaterThanOrEquals(DateTimeOffset createdAt)
            => InternalGreaterThanOrEquals(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is LESS THAN the provided one
        /// </summary>
        public FieldFilterRule LessThan(DateTime createdAt) => InternalLessThan(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is LESS THAN the provided one
        /// </summary>
        public FieldFilterRule LessThan(DateTimeOffset createdAt) => InternalLessThan(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is LESS THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule LessThanOrEquals(DateTime createdAt) => InternalLessThanOrEquals(createdAt);

        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is LESS THAN OR EQUAL to the provided one
        /// </summary>
        public FieldFilterRule LessThanOrEquals(DateTimeOffset createdAt)
            => InternalLessThanOrEquals(createdAt);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is EQUAL to ANY of provided user Id
        /// </summary>
        public FieldFilterRule In(params DateTime[] createdAt) => InternalIn(createdAt);
        
        /// <summary>
        /// Return only calls where <see cref="IStreamCall.EndedAt"/> is EQUAL to ANY of provided user Id
        /// </summary>
        public FieldFilterRule In(params DateTimeOffset[] createdAt) => InternalIn(createdAt);
    }
}