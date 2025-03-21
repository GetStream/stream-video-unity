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
    internal partial class AudioSettingsResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("access_request_enabled", Required = Newtonsoft.Json.Required.Default)]
        public bool AccessRequestEnabled { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("default_device", Required = Newtonsoft.Json.Required.Default)]
        [Newtonsoft.Json.JsonConverter(typeof(StreamVideo.Core.Serialization.EnumeratedStructConverter<AudioSettingsDefaultDeviceInternalEnumDTO>))]
        public AudioSettingsDefaultDeviceInternalEnumDTO DefaultDevice { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("mic_default_on", Required = Newtonsoft.Json.Required.Default)]
        public bool MicDefaultOn { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("noise_cancellation", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public NoiseCancellationSettingsInternalDTO NoiseCancellation { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("opus_dtx_enabled", Required = Newtonsoft.Json.Required.Default)]
        public bool OpusDtxEnabled { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("redundant_coding_enabled", Required = Newtonsoft.Json.Required.Default)]
        public bool RedundantCodingEnabled { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("speaker_default_on", Required = Newtonsoft.Json.Required.Default)]
        public bool SpeakerDefaultOn { get; set; } = default!;

    }

}

