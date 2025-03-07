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
    internal partial class ChannelConfigWithInfoInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("allowed_flag_reasons", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.List<string> AllowedFlagReasons { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("automod", Required = Newtonsoft.Json.Required.Default)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ChannelConfigWithInfoAutomodInternalEnum Automod { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("automod_behavior", Required = Newtonsoft.Json.Required.Default)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ChannelConfigWithInfoAutomodBehaviorInternalEnum AutomodBehavior { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("automod_thresholds", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ThresholdsInternalDTO AutomodThresholds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("blocklist", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Blocklist { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("blocklist_behavior", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ChannelConfigWithInfoBlocklistBehaviorInternalEnum BlocklistBehavior { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("blocklists", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.List<BlockListOptionsInternalDTO> Blocklists { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("commands", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<CommandInternalDTO?> Commands { get; set; } = new System.Collections.Generic.List<CommandInternalDTO?>();

        [Newtonsoft.Json.JsonProperty("connect_events", Required = Newtonsoft.Json.Required.Default)]
        public bool ConnectEvents { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("custom_events", Required = Newtonsoft.Json.Required.Default)]
        public bool CustomEvents { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("grants", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> Grants { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("mark_messages_pending", Required = Newtonsoft.Json.Required.Default)]
        public bool MarkMessagesPending { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_message_length", Required = Newtonsoft.Json.Required.Default)]
        public int MaxMessageLength { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("mutes", Required = Newtonsoft.Json.Required.Default)]
        public bool Mutes { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default)]
        public string Name { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("partition_size", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int PartitionSize { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("partition_ttl", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int PartitionTtl { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("polls", Required = Newtonsoft.Json.Required.Default)]
        public bool Polls { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("push_notifications", Required = Newtonsoft.Json.Required.Default)]
        public bool PushNotifications { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("quotes", Required = Newtonsoft.Json.Required.Default)]
        public bool Quotes { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("reactions", Required = Newtonsoft.Json.Required.Default)]
        public bool Reactions { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("read_events", Required = Newtonsoft.Json.Required.Default)]
        public bool ReadEvents { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("reminders", Required = Newtonsoft.Json.Required.Default)]
        public bool Reminders { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("replies", Required = Newtonsoft.Json.Required.Default)]
        public bool Replies { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("search", Required = Newtonsoft.Json.Required.Default)]
        public bool Search { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("typing_events", Required = Newtonsoft.Json.Required.Default)]
        public bool TypingEvents { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("updated_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset UpdatedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("uploads", Required = Newtonsoft.Json.Required.Default)]
        public bool Uploads { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("url_enrichment", Required = Newtonsoft.Json.Required.Default)]
        public bool UrlEnrichment { get; set; } = default!;

    }

}

