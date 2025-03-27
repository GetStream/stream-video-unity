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
    internal partial class StartClosedCaptionsRequestInternalDTO
    {
        /// <summary>
        /// Enable transcriptions along with closed captions
        /// </summary>
        [Newtonsoft.Json.JsonProperty("enable_transcription", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool EnableTranscription { get; set; } = default!;

        /// <summary>
        /// Which external storage to use for transcriptions (only applicable if enable_transcription is true)
        /// </summary>
        [Newtonsoft.Json.JsonProperty("external_storage", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string ExternalStorage { get; set; } = default!;

        /// <summary>
        /// The spoken language in the call, if not provided the language defined in the transcription settings will be used
        /// </summary>
        [Newtonsoft.Json.JsonProperty("language", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Language { get; set; } = default!;

    }

}

