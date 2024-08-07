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
    /// A custom event, this event is used to send custom events to other participants in the call.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CustomVideoEventInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("call_cid", Required = Newtonsoft.Json.Required.Default)]
        public string CallCid { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        /// <summary>
        /// Custom data for this object
        /// </summary>
        [Newtonsoft.Json.JsonProperty("custom", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.Dictionary<string, object> Custom { get; set; } = default!;

        /// <summary>
        /// The type of event, "custom" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default)]
        public string Type { get; set; } = "custom";

        [Newtonsoft.Json.JsonProperty("user", Required = Newtonsoft.Json.Required.Default)]
        public UserResponseInternalDTO User { get; set; } = new UserResponseInternalDTO();

    }

}

