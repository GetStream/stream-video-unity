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
        [Newtonsoft.Json.JsonProperty("aggregated", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public AggregatedStatsInternalDTO Aggregated { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("call_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int CallDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("call_status", Required = Newtonsoft.Json.Required.Default)]
        public string CallStatus { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("call_timeline", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public CallTimelineInternalDTO CallTimeline { get; set; } = default!;

        /// <summary>
        /// Duration of the request in milliseconds
        /// </summary>
        [Newtonsoft.Json.JsonProperty("duration", Required = Newtonsoft.Json.Required.Default)]
        public string Duration { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("jitter", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO Jitter { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("latency", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO Latency { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_freezes_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int MaxFreezesDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_participants", Required = Newtonsoft.Json.Required.Default)]
        public int MaxParticipants { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_total_quality_limitation_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int MaxTotalQualityLimitationDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("participant_report", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<UserStatsInternalDTO?> ParticipantReport { get; set; } = new System.Collections.Generic.List<UserStatsInternalDTO?>();

        [Newtonsoft.Json.JsonProperty("publishing_participants", Required = Newtonsoft.Json.Required.Default)]
        public int PublishingParticipants { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("quality_score", Required = Newtonsoft.Json.Required.Default)]
        public int QualityScore { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("sfu_count", Required = Newtonsoft.Json.Required.Default)]
        public int SfuCount { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("sfus", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<SFULocationResponseInternalDTO> Sfus { get; set; } = new System.Collections.Generic.List<SFULocationResponseInternalDTO>();

    }

}
