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
    internal partial class DeviceInternalDTO
    {
        /// <summary>
        /// Date/time of creation
        /// </summary>
        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        /// <summary>
        /// Whether device is disabled or not
        /// </summary>
        [Newtonsoft.Json.JsonProperty("disabled", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Disabled { get; set; } = default!;

        /// <summary>
        /// Reason explaining why device had been disabled
        /// </summary>
        [Newtonsoft.Json.JsonProperty("disabled_reason", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string DisabledReason { get; set; } = default!;

        /// <summary>
        /// Device ID
        /// </summary>
        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Default)]
        public string Id { get; set; } = default!;

        /// <summary>
        /// Push provider
        /// </summary>
        [Newtonsoft.Json.JsonProperty("push_provider", Required = Newtonsoft.Json.Required.Default)]
        public string PushProvider { get; set; } = default!;

        /// <summary>
        /// Push provider name
        /// </summary>
        [Newtonsoft.Json.JsonProperty("push_provider_name", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string PushProviderName { get; set; } = default!;

        /// <summary>
        /// User ID
        /// </summary>
        [Newtonsoft.Json.JsonProperty("user_id", Required = Newtonsoft.Json.Required.Default)]
        public string UserId { get; set; } = default!;

        /// <summary>
        /// When true the token is for Apple VoIP push notifications
        /// </summary>
        [Newtonsoft.Json.JsonProperty("voip", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Voip { get; set; } = default!;

    }

}

