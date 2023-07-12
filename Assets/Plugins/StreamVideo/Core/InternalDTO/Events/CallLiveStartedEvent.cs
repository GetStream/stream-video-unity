//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------


using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Events
{
    using System = global::System;

    /// <summary>
    /// This event is sent when a call is started. Clients receiving this event should start the call.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    public partial class CallLiveStartedEvent
    {
        /// <summary>
        /// Call object
        /// </summary>
        [Newtonsoft.Json.JsonProperty("call", Required = Newtonsoft.Json.Required.Always)]
        public CallResponse Call { get; set; } = new CallResponse();

        [Newtonsoft.Json.JsonProperty("call_cid", Required = Newtonsoft.Json.Required.Always)]
        public string CallCid { get; set; }

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Always)]
        public System.DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// The type of event: "call.live_started" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; } = "call.live_started";

    }

}

