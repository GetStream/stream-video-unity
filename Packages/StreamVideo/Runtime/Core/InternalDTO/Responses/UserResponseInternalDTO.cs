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
    internal partial class UserResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("banned", Required = Newtonsoft.Json.Required.Default)]
        public bool Banned { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("blocked_user_ids", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<string> BlockedUserIds { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Date/time of creation
        /// </summary>
        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("custom", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.Dictionary<string, object> Custom { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("deactivated_at", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.DateTimeOffset DeactivatedAt { get; set; } = default!;

        /// <summary>
        /// Date/time of deletion
        /// </summary>
        [Newtonsoft.Json.JsonProperty("deleted_at", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.DateTimeOffset DeletedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Default)]
        public string Id { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("image", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Image { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("language", Required = Newtonsoft.Json.Required.Default)]
        public string Language { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("last_active", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.DateTimeOffset LastActive { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Name { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("online", Required = Newtonsoft.Json.Required.Default)]
        public bool Online { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("revoke_tokens_issued_before", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.DateTimeOffset RevokeTokensIssuedBefore { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("role", Required = Newtonsoft.Json.Required.Default)]
        public string Role { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("teams", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.List<string> Teams { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Date/time of the last update
        /// </summary>
        [Newtonsoft.Json.JsonProperty("updated_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset UpdatedAt { get; set; } = default!;

    }

}

