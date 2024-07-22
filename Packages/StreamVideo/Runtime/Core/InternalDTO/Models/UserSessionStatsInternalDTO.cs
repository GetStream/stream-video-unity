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
    internal partial class UserSessionStatsInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("browser", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Browser { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("browser_version", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string BrowserVersion { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("current_ip", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string CurrentIp { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("current_sfu", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string CurrentSfu { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("device_model", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string DeviceModel { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("device_version", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string DeviceVersion { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("distance_to_sfu_kilometers", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float DistanceToSfuKilometers { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("freeze_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int FreezeDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("geolocation", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public GeolocationResultInternalDTO Geolocation { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("jitter", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO Jitter { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("latency", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO Latency { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_fir_per_second", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float MaxFirPerSecond { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_freeze_fraction", Required = Newtonsoft.Json.Required.Default)]
        public float MaxFreezeFraction { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_freezes_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int MaxFreezesDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_freezes_per_second", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float MaxFreezesPerSecond { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_nack_per_second", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float MaxNackPerSecond { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_pli_per_second", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float MaxPliPerSecond { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_publishing_video_quality", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public VideoQualityInternalDTO MaxPublishingVideoQuality { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("max_receiving_video_quality", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public VideoQualityInternalDTO MaxReceivingVideoQuality { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("os", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Os { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("os_version", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string OsVersion { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("packet_loss_fraction", Required = Newtonsoft.Json.Required.Default)]
        public float PacketLossFraction { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("pub_sub_hints", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public MediaPubSubHintInternalDTO PubSubHints { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("published_tracks", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.List<PublishedTrackInfoInternalDTO> PublishedTracks { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publisher_audio_mos", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public MOSStatsInternalDTO PublisherAudioMos { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publisher_jitter", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO PublisherJitter { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publisher_latency", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO PublisherLatency { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publisher_noise_cancellation_seconds", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float PublisherNoiseCancellationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publisher_packet_loss_fraction", Required = Newtonsoft.Json.Required.Default)]
        public float PublisherPacketLossFraction { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publisher_quality_limitation_fraction", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float PublisherQualityLimitationFraction { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publisher_video_quality_limitation_duration_seconds", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.Dictionary<string, float> PublisherVideoQualityLimitationDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publishing_audio_codec", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string PublishingAudioCodec { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publishing_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int PublishingDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("publishing_video_codec", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string PublishingVideoCodec { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("quality_score", Required = Newtonsoft.Json.Required.Default)]
        public float QualityScore { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("receiving_audio_codec", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string ReceivingAudioCodec { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("receiving_duration_seconds", Required = Newtonsoft.Json.Required.Default)]
        public int ReceivingDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("receiving_video_codec", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string ReceivingVideoCodec { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("sdk", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Sdk { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("sdk_version", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string SdkVersion { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("session_id", Required = Newtonsoft.Json.Required.Default)]
        public string SessionId { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("subscriber_audio_mos", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public MOSStatsInternalDTO SubscriberAudioMos { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("subscriber_jitter", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO SubscriberJitter { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("subscriber_latency", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TimeStatsInternalDTO SubscriberLatency { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("subscriber_video_quality_throttled_duration_seconds", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public float SubscriberVideoQualityThrottledDurationSeconds { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("subsessions", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.List<SubsessionInternalDTO?> Subsessions { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("timeline", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public CallTimelineInternalDTO Timeline { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("total_pixels_in", Required = Newtonsoft.Json.Required.Default)]
        public int TotalPixelsIn { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("total_pixels_out", Required = Newtonsoft.Json.Required.Default)]
        public int TotalPixelsOut { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("truncated", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Truncated { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("webrtc_version", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string WebrtcVersion { get; set; } = default!;

    }

}

