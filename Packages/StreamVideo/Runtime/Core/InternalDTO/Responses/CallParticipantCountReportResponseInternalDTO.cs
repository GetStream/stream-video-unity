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
    internal partial class CallParticipantCountReportResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("daily", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<DailyAggregateCallParticipantCountReportResponseInternalDTO> Daily { get; set; } = new System.Collections.Generic.List<DailyAggregateCallParticipantCountReportResponseInternalDTO>();

    }

}

