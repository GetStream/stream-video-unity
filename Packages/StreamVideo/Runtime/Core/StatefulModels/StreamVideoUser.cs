using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.StatefulModels
{
    internal class StreamVideoUser : StreamStatefulModelBase<StreamVideoUser>,
        IUpdateableFrom<UserResponseInternalDTO, StreamVideoUser>,
        IUpdateableFrom<OwnUserResponseInternalDTO, StreamVideoUser>,
        IStreamVideoUser
    {
        #region State 
        //StreamTodo: wrap state in a separate class. This will also help with validation because we test via reflection whether:
        // every updateFrom method uses all of the fields from DTO (this way if DTO gets extended we ensure that we've updated all of the mapping as well)

        /// <summary>
        /// Date/time of creation
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Date/time of deletion
        /// </summary>
        public DateTimeOffset DeletedAt { get; private set; }

        public string Id { get; private set; }

        public string Image { get; private set; }

        public string Name { get; private set; }

        public string Role { get; private set; }

        public IReadOnlyList<string> Teams => _teams;

        /// <summary>
        /// Date/time of the last update
        /// </summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        #endregion

        internal StreamVideoUser(string uniqueId, ICacheRepository<StreamVideoUser> repository,
            IStatefulModelContext context)
            : base(uniqueId, repository, context)
        {
        }

        protected override string InternalUniqueId { get => Id; set => Id = value; }
        protected override StreamVideoUser Self => this;

        protected override Task UploadCustomDataAsync()
        {
            //StreamTodo: implement user custom data writing once the API exposes such functionality
            throw new NotImplementedException();
        }

        #region State

        private readonly List<string> _teams = new List<string>();

        #endregion

        void IUpdateableFrom<UserResponseInternalDTO, StreamVideoUser>.UpdateFromDto(UserResponseInternalDTO dto, ICache cache)
        {
            CreatedAt = GetOrDefault(dto.CreatedAt, CreatedAt);
            LoadCustomData(dto.Custom);
            DeletedAt = GetOrDefault(dto.DeletedAt, DeletedAt);
            Id = GetOrDefault(dto.Id, Id);
            Image = GetOrDefault(dto.Image, Image);
            Name = GetOrDefault(dto.Name, Name);
            Role = GetOrDefault(dto.Role, Role);
            _teams.TryReplaceValuesFromDto(dto.Teams);
            UpdatedAt = GetOrDefault(dto.UpdatedAt, UpdatedAt);
        }
        
        void IUpdateableFrom<OwnUserResponseInternalDTO, StreamVideoUser>.UpdateFromDto(OwnUserResponseInternalDTO dto, ICache cache)
        {
            CreatedAt = GetOrDefault(dto.CreatedAt, CreatedAt);
            LoadCustomData(dto.Custom);
            DeletedAt = GetOrDefault(dto.DeletedAt, DeletedAt);
            Id = GetOrDefault(dto.Id, Id);
            Image = GetOrDefault(dto.Image, Image);
            Name = GetOrDefault(dto.Name, Name);
            Role = GetOrDefault(dto.Role, Role);
            _teams.TryReplaceValuesFromDto(dto.Teams);
            UpdatedAt = GetOrDefault(dto.UpdatedAt, UpdatedAt);
        }
    }
}