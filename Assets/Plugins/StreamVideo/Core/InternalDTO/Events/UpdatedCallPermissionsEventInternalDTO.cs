//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#nullable enable


using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Events
{
    using System = global::System;

    /// <summary>
    /// This event is sent to notify about permission changes for a user, clients receiving this event should update their UI accordingly
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class UpdatedCallPermissionsEventInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("call_cid", Required = Newtonsoft.Json.Required.Always)]
        public string CallCid { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Always)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        /// <summary>
        /// The capabilities of the current user
        /// </summary>
        [Newtonsoft.Json.JsonProperty("own_capabilities", Required = Newtonsoft.Json.Required.Always, ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public System.Collections.Generic.List<OwnCapabilityInternalEnum> OwnCapabilities { get; set; } = new System.Collections.Generic.List<OwnCapabilityInternalEnum>();

        /// <summary>
        /// The type of event: "call.permissions_updated" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; } = "call.permissions_updated";

        /// <summary>
        /// The user who received the new permissions
        /// </summary>
        [Newtonsoft.Json.JsonProperty("user", Required = Newtonsoft.Json.Required.Always)]
        public UserResponseInternalDTO User { get; set; } = new UserResponseInternalDTO();

    }

}

