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
    internal partial class MOSStatsInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("average_score", Required = Newtonsoft.Json.Required.Default)]
        public float AverageScore { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("histogram_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<float> HistogramDurationSeconds { get; set; } = new System.Collections.Generic.List<float>();

        [Newtonsoft.Json.JsonProperty("max_score", Required = Newtonsoft.Json.Required.Default)]
        public float MaxScore { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("min_score", Required = Newtonsoft.Json.Required.Default)]
        public float MinScore { get; set; } = default!;

    }

}

