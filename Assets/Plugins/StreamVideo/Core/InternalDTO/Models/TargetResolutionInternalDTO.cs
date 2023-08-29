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
    internal partial class TargetResolutionInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("bitrate", Required = Newtonsoft.Json.Required.Always)]
        public int Bitrate { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("height", Required = Newtonsoft.Json.Required.Always)]
        public int Height { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("width", Required = Newtonsoft.Json.Required.Always)]
        public int Width { get; set; } = default!;

    }

}

