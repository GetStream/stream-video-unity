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
    internal partial class CallSettingsResponseInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("audio", Required = Newtonsoft.Json.Required.Always)]
        public AudioSettingsInternalDTO Audio { get; set; } = new AudioSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("backstage", Required = Newtonsoft.Json.Required.Always)]
        public BackstageSettingsInternalDTO Backstage { get; set; } = new BackstageSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("broadcasting", Required = Newtonsoft.Json.Required.Always)]
        public BroadcastSettingsInternalDTO Broadcasting { get; set; } = new BroadcastSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("geofencing", Required = Newtonsoft.Json.Required.Always)]
        public GeofenceSettingsInternalDTO Geofencing { get; set; } = new GeofenceSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("recording", Required = Newtonsoft.Json.Required.Always)]
        public RecordSettingsInternalDTO Recording { get; set; } = new RecordSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("ring", Required = Newtonsoft.Json.Required.Always)]
        public RingSettingsInternalDTO Ring { get; set; } = new RingSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("screensharing", Required = Newtonsoft.Json.Required.Always)]
        public ScreensharingSettingsInternalDTO Screensharing { get; set; } = new ScreensharingSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("transcription", Required = Newtonsoft.Json.Required.Always)]
        public TranscriptionSettingsInternalDTO Transcription { get; set; } = new TranscriptionSettingsInternalDTO();

        [Newtonsoft.Json.JsonProperty("video", Required = Newtonsoft.Json.Required.Always)]
        public VideoSettingsInternalDTO Video { get; set; } = new VideoSettingsInternalDTO();

    }

}

