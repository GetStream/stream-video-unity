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
    /// This event is sent when call broadcasting has started
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CallBroadcastingStartedEventInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("call_cid", Required = Newtonsoft.Json.Required.Always)]
        public string CallCid { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Always)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("hls_playlist_url", Required = Newtonsoft.Json.Required.Always)]
        public string HlsPlaylistUrl { get; set; } = default!;

        /// <summary>
        /// The type of event: "call.broadcasting_started" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; } = "call.broadcasting_started";

    }

}
