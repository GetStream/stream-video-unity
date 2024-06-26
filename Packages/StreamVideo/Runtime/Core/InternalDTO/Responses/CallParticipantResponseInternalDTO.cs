//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#nullable enable


using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Responses
{
    using System = global::System;

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CallParticipantResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("joined_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset JoinedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("role", Required = Newtonsoft.Json.Required.Default)]
        public string Role { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("user", Required = Newtonsoft.Json.Required.Default)]
        public UserResponseInternalDTO User { get; set; } = new UserResponseInternalDTO();

        [Newtonsoft.Json.JsonProperty("user_session_id", Required = Newtonsoft.Json.Required.Default)]
        public string UserSessionId { get; set; } = default!;

    }

}

