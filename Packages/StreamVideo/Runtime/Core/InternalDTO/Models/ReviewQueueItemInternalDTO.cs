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
    internal partial class ReviewQueueItemInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("actions", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<ActionLogInternalDTO?> Actions { get; set; } = new System.Collections.Generic.List<ActionLogInternalDTO?>();

        [Newtonsoft.Json.JsonProperty("assigned_to", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public UserObjectInternalDTO AssignedTo { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("bans", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<BanInternalDTO?> Bans { get; set; } = new System.Collections.Generic.List<BanInternalDTO?>();

        [Newtonsoft.Json.JsonProperty("completed_at", Required = Newtonsoft.Json.Required.Default)]
        public NullTimeInternalDTO CompletedAt { get; set; } = new NullTimeInternalDTO();

        [Newtonsoft.Json.JsonProperty("content_changed", Required = Newtonsoft.Json.Required.Default)]
        public bool ContentChanged { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("entity_creator", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public UserObjectInternalDTO EntityCreator { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("entity_id", Required = Newtonsoft.Json.Required.Default)]
        public string EntityId { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("entity_type", Required = Newtonsoft.Json.Required.Default)]
        public string EntityType { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("feeds_v2_activity", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public EnrichedActivityInternalDTO FeedsV2Activity { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("feeds_v2_reaction", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ReactionInternalDTO? FeedsV2Reaction { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("flags", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<Flag2InternalDTO?> Flags { get; set; } = new System.Collections.Generic.List<Flag2InternalDTO?>();

        [Newtonsoft.Json.JsonProperty("has_image", Required = Newtonsoft.Json.Required.Default)]
        public bool HasImage { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("has_text", Required = Newtonsoft.Json.Required.Default)]
        public bool HasText { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("has_video", Required = Newtonsoft.Json.Required.Default)]
        public bool HasVideo { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Default)]
        public string Id { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("languages", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<string> Languages { get; set; } = new System.Collections.Generic.List<string>();

        [Newtonsoft.Json.JsonProperty("message", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public MessageInternalDTO Message { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("moderation_payload", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ModerationPayloadInternalDTO ModerationPayload { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("moderation_payload_hash", Required = Newtonsoft.Json.Required.Default)]
        public string ModerationPayloadHash { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("recommended_action", Required = Newtonsoft.Json.Required.Default)]
        public string RecommendedAction { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("reviewed_at", Required = Newtonsoft.Json.Required.Default)]
        public NullTimeInternalDTO ReviewedAt { get; set; } = new NullTimeInternalDTO();

        [Newtonsoft.Json.JsonProperty("reviewed_by", Required = Newtonsoft.Json.Required.Default)]
        public string ReviewedBy { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("severity", Required = Newtonsoft.Json.Required.Default)]
        public int Severity { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("status", Required = Newtonsoft.Json.Required.Default)]
        public string Status { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("updated_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset UpdatedAt { get; set; } = default!;

    }

}

