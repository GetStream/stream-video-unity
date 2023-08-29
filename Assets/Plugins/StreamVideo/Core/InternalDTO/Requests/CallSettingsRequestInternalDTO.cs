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
    internal partial class CallSettingsRequestInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("audio", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public AudioSettingsRequestInternalDTO Audio { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("backstage", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public BackstageSettingsRequestInternalDTO Backstage { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("broadcasting", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public BroadcastSettingsRequestInternalDTO Broadcasting { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("geofencing", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public GeofenceSettingsRequestInternalDTO Geofencing { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("recording", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public RecordSettingsRequestInternalDTO Recording { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("ring", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public RingSettingsRequestInternalDTO Ring { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("screensharing", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ScreensharingSettingsRequestInternalDTO Screensharing { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("transcription", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TranscriptionSettingsRequestInternalDTO Transcription { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("video", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public VideoSettingsRequestInternalDTO Video { get; set; } = default!;

    }

}

