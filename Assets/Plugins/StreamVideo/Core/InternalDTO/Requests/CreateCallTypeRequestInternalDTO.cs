//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#nullable enable


using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Requests
{
    using System = global::System;

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class CreateCallTypeRequestInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("grants", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> Grants { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Always)]
        public string Name { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("notification_settings", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public NotificationSettingsRequestInternalDTO NotificationSettings { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("settings", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public CallSettingsRequestInternalDTO Settings { get; set; } = default!;

    }

}

