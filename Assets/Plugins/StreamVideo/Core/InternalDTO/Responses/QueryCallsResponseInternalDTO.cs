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
    internal partial class QueryCallsResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("calls", Required = Newtonsoft.Json.Required.Always)]
        public System.Collections.Generic.List<CallStateResponseFieldsInternalDTO> Calls { get; set; } = new System.Collections.Generic.List<CallStateResponseFieldsInternalDTO>();

        [Newtonsoft.Json.JsonProperty("duration", Required = Newtonsoft.Json.Required.Always)]
        public string Duration { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("next", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Next { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("prev", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prev { get; set; } = default!;

    }

}

