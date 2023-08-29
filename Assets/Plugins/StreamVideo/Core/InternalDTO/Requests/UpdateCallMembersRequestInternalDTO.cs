//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#nullable enable


using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Requests
{
    using System = global::System;

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class UpdateCallMembersRequestInternalDTO
    {
        /// <summary>
        /// List of userID to remove
        /// </summary>
        [Newtonsoft.Json.JsonProperty("remove_members", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.List<string> RemoveMembers { get; set; } = default!;

        /// <summary>
        /// List of members to update or insert
        /// </summary>
        [Newtonsoft.Json.JsonProperty("update_members", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.List<MemberRequestInternalDTO> UpdateMembers { get; set; } = default!;

    }

}

