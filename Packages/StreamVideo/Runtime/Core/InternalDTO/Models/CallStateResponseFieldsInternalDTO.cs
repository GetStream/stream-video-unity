//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#nullable enable


using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Events;

namespace StreamVideo.Core.InternalDTO.Models
{
    using System = global::System;

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CallStateResponseFieldsInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("call", Required = Newtonsoft.Json.Required.Default)]
        public CallResponseInternalDTO Call { get; set; } = new CallResponseInternalDTO();

        /// <summary>
        /// List of call members
        /// </summary>
        [Newtonsoft.Json.JsonProperty("members", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<MemberResponseInternalDTO> Members { get; set; } = new System.Collections.Generic.List<MemberResponseInternalDTO>();

        /// <summary>
        /// Current user membership object
        /// </summary>
        [Newtonsoft.Json.JsonProperty("membership", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public MemberResponseInternalDTO Membership { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("own_capabilities", Required = Newtonsoft.Json.Required.Default, ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public System.Collections.Generic.List<OwnCapabilityInternalEnum> OwnCapabilities { get; set; } = new System.Collections.Generic.List<OwnCapabilityInternalEnum>();

    }

}

