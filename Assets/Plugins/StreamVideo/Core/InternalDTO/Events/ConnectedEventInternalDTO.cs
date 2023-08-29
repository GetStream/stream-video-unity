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
    /// This event is sent when the WS connection is established and authenticated, this event contains the full user object as it is stored on the server
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class ConnectedEventInternalDTO
    {
        /// <summary>
        /// The connection_id for this client
        /// </summary>
        [Newtonsoft.Json.JsonProperty("connection_id", Required = Newtonsoft.Json.Required.Always)]
        public string ConnectionId { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Always)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        /// <summary>
        /// The full user object as it is stored on the server
        /// </summary>
        [Newtonsoft.Json.JsonProperty("me", Required = Newtonsoft.Json.Required.Always)]
        public OwnUserResponseInternalDTO Me { get; set; } = new OwnUserResponseInternalDTO();

        /// <summary>
        /// The type of event: "connection.ok" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; } = "connection.ok";

    }

}

