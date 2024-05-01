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
    internal partial class GetCallTypeResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset CreatedAt { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("duration", Required = Newtonsoft.Json.Required.Default)]
        public string Duration { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("external_storage", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string ExternalStorage { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("grants", Required = Newtonsoft.Json.Required.Default)]
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> Grants { get; set; } = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();

        [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default)]
        public string Name { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("notification_settings", Required = Newtonsoft.Json.Required.Default)]
        public NotificationSettingsInternalDTO NotificationSettings { get; set; } = new NotificationSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("settings", Required = Newtonsoft.Json.Required.Default)]
        public CallSettingsResponseInternalDTO Settings { get; set; } = new CallSettingsResponseInternalDTO();

        [Newtonsoft.Json.JsonProperty("updated_at", Required = Newtonsoft.Json.Required.Default)]
        public System.DateTimeOffset UpdatedAt { get; set; } = default!;

    }

}

