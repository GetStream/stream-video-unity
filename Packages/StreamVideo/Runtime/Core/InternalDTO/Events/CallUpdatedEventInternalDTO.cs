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
    /// This event is sent when a call is updated, clients should use this update the local state of the call.
    /// <br/>This event also contains the capabilities by role for the call, clients should update the own_capability for the current.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CallUpdatedEventInternalDTO
    {
        /// <summary>
        /// Call object
        /// </summary>
        [Newtonsoft.Json.JsonProperty("call", Required = Newtonsoft.Json.Required.Default)]
        public CallResponseInternalDTO Call { get; set; } = new CallResponseInternalDTO();

        [Newtonsoft.Json.JsonProperty("call_cid", Required = Newtonsoft.Json.Required.Default)]
        public string CallCid { get; set; } = default!;

        /// <summary>
        /// The capabilities by role for this call
        /// </summary>
        [Newtonsoft.Json.JsonProperty("capabilities_by_role", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> CapabilitiesByRole { get; set; } = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        /// <summary>
        /// The type of event: "call.ended" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default)]
        public string Type { get; set; } = "call.updated";

    }

}

