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
    internal partial class SFUResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("edge_name", Required = Newtonsoft.Json.Required.Always)]
        public string EdgeName { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("url", Required = Newtonsoft.Json.Required.Always)]
        public string Url { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("ws_endpoint", Required = Newtonsoft.Json.Required.Always)]
        public string WsEndpoint { get; set; } = default!;

    }

}

