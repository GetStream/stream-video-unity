//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#nullable enable


using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Events;

namespace StreamVideo.Core.InternalDTO.Models
{
    using System = global::System;

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class AudioSettingsInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("access_request_enabled", Required = Newtonsoft.Json.Required.Always)]
        public bool AccessRequestEnabled { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("default_device", Required = Newtonsoft.Json.Required.Always)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public AudioSettingsDefaultDeviceInternalEnum DefaultDevice { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("mic_default_on", Required = Newtonsoft.Json.Required.Always)]
        public bool MicDefaultOn { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("opus_dtx_enabled", Required = Newtonsoft.Json.Required.Always)]
        public bool OpusDtxEnabled { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("redundant_coding_enabled", Required = Newtonsoft.Json.Required.Always)]
        public bool RedundantCodingEnabled { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("speaker_default_on", Required = Newtonsoft.Json.Required.Always)]
        public bool SpeakerDefaultOn { get; set; } = default!;

    }

}
