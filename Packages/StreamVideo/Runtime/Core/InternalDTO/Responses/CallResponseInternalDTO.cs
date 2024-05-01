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

    /// <summary>
    /// Represents a call
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CallResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("backstage", Required = Newtonsoft.Json.Required.Default)]
        public bool Backstage { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("blocked_user_ids", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<string> BlockedUserIds { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// The unique identifier for a call (&lt;type&gt;:&lt;id&gt;)
        /// </summary>
        [Newtonsoft.Json.JsonProperty("cid", Required = Newtonsoft.Json.Required.Default)]
        public string Cid { get; set; } = default!;

        /// <summary>
        /// Date/time of creation
        /// </summary>
        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        /// <summary>
        /// The user that created the call
        /// </summary>
        [Newtonsoft.Json.JsonProperty("created_by", Required = Newtonsoft.Json.Required.Default)]
        public UserResponseInternalDTO CreatedBy { get; set; } = new UserResponseInternalDTO();

        [Newtonsoft.Json.JsonProperty("current_session_id", Required = Newtonsoft.Json.Required.Default)]
        public string CurrentSessionId { get; set; } = default!;

        /// <summary>
        /// Custom data for this object
        /// </summary>
        [Newtonsoft.Json.JsonProperty("custom", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.Dictionary<string, object> Custom { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("egress", Required = Newtonsoft.Json.Required.Default)]
        public EgressResponseInternalDTO Egress { get; set; } = new EgressResponseInternalDTO();

        /// <summary>
        /// Date/time when the call ended
        /// </summary>
        [Newtonsoft.Json.JsonProperty("ended_at", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.DateTimeOffset EndedAt { get; set; } = default!;

        /// <summary>
        /// Call ID
        /// </summary>
        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Default)]
        public string Id { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("ingress", Required = Newtonsoft.Json.Required.Default)]
        public CallIngressResponseInternalDTO Ingress { get; set; } = new CallIngressResponseInternalDTO();

        [Newtonsoft.Json.JsonProperty("recording", Required = Newtonsoft.Json.Required.Default)]
        public bool Recording { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("session", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public CallSessionResponseInternalDTO Session { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("settings", Required = Newtonsoft.Json.Required.Default)]
        public CallSettingsResponseInternalDTO Settings { get; set; } = new CallSettingsResponseInternalDTO();

        /// <summary>
        /// Date/time when the call will start
        /// </summary>
        [Newtonsoft.Json.JsonProperty("starts_at", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.DateTimeOffset StartsAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("team", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Team { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("thumbnails", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ThumbnailResponseInternalDTO Thumbnails { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("transcribing", Required = Newtonsoft.Json.Required.Default)]
        public bool Transcribing { get; set; } = default!;

        /// <summary>
        /// The type of call
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default)]
        public string Type { get; set; } = default!;

        /// <summary>
        /// Date/time of the last update
        /// </summary>
        [Newtonsoft.Json.JsonProperty("updated_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset UpdatedAt { get; set; } = default!;

    }

}

