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

    /// <summary>
    /// This event is sent when a call member is muted
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CallUserMutedInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("call_cid", Required = Newtonsoft.Json.Required.Always)]
        public string CallCid { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Always)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("from_user_id", Required = Newtonsoft.Json.Required.Always)]
        public string FromUserId { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("muted_user_ids", Required = Newtonsoft.Json.Required.Always)]
        public System.Collections.Generic.List<string> MutedUserIds { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// The type of event: "call.user_muted" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; } = "call.user_muted";

    }

}

