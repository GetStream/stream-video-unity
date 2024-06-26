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
    internal partial class GetCallStatsResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("average_jitter", Required = Newtonsoft.Json.Required.Default)]
        public float AverageJitter { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("average_latency", Required = Newtonsoft.Json.Required.Default)]
        public float AverageLatency { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("call_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int CallDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("call_timeline", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public CallTimelineInternalDTO CallTimeline { get; set; } = default!;

        /// <summary>
        /// Duration of the request in human-readable format
        /// </summary>
        [Newtonsoft.Json.JsonProperty("duration", Required = Newtonsoft.Json.Required.Default)]
        public string Duration { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_jitter", Required = Newtonsoft.Json.Required.Default)]
        public float MaxJitter { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_latency", Required = Newtonsoft.Json.Required.Default)]
        public float MaxLatency { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_participants", Required = Newtonsoft.Json.Required.Default)]
        public int MaxParticipants { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("participant_report", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.Dictionary<string, UserStatsInternalDTO> ParticipantReport { get; set; } = new System.Collections.Generic.Dictionary<string, UserStatsInternalDTO>();

        [Newtonsoft.Json.JsonProperty("publishing_participants", Required = Newtonsoft.Json.Required.Default)]
        public int PublishingParticipants { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("quality_score", Required = Newtonsoft.Json.Required.Default)]
        public int QualityScore { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("sfu_count", Required = Newtonsoft.Json.Required.Default)]
        public int SfuCount { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("sfus", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<SFULocationResponseInternalDTO> Sfus { get; set; } = new System.Collections.Generic.List<SFULocationResponseInternalDTO>();

        [Newtonsoft.Json.JsonProperty("total_freezes_duration", Required = Newtonsoft.Json.Required.Default)]
        public float TotalFreezesDuration { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("total_quality_limitation_duration", Required = Newtonsoft.Json.Required.Default)]
        public float TotalQualityLimitationDuration { get; set; } = default!;

    }

}

