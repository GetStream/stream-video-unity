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
    internal partial class GetOrCreateCallResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("blocked_users", Required = Newtonsoft.Json.Required.Always)]
        public System.Collections.Generic.List<UserResponseInternalDTO> BlockedUsers { get; set; } = new System.Collections.Generic.List<UserResponseInternalDTO>();

        [Newtonsoft.Json.JsonProperty("call", Required = Newtonsoft.Json.Required.Always)]
        public CallResponseInternalDTO Call { get; set; } = new CallResponseInternalDTO();

        [Newtonsoft.Json.JsonProperty("created", Required = Newtonsoft.Json.Required.Always)]
        public bool Created { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("duration", Required = Newtonsoft.Json.Required.Always)]
        public string Duration { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("members", Required = Newtonsoft.Json.Required.Always)]
        public System.Collections.Generic.List<MemberResponseInternalDTO> Members { get; set; } = new System.Collections.Generic.List<MemberResponseInternalDTO>();

        [Newtonsoft.Json.JsonProperty("membership", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public MemberResponseInternalDTO Membership { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("own_capabilities", Required = Newtonsoft.Json.Required.Always, ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public System.Collections.Generic.List<OwnCapabilityInternalEnum> OwnCapabilities { get; set; } = new System.Collections.Generic.List<OwnCapabilityInternalEnum>();

    }

}

