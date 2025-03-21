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

    internal readonly struct FrameRecordingSettingsResponseModeInternalEnumDTO : System.IEquatable<FrameRecordingSettingsResponseModeInternalEnumDTO>, StreamVideo.Core.LowLevelClient.IEnumeratedStruct<FrameRecordingSettingsResponseModeInternalEnumDTO>
    {
        public string Value { get; }

        public static readonly FrameRecordingSettingsResponseModeInternalEnumDTO Available = new FrameRecordingSettingsResponseModeInternalEnumDTO("available");
        public static readonly FrameRecordingSettingsResponseModeInternalEnumDTO Disabled = new FrameRecordingSettingsResponseModeInternalEnumDTO("disabled");
        public static readonly FrameRecordingSettingsResponseModeInternalEnumDTO Auto_On = new FrameRecordingSettingsResponseModeInternalEnumDTO("auto-on");

        public FrameRecordingSettingsResponseModeInternalEnumDTO(string value)
        {
            Value = value;
        }

        public FrameRecordingSettingsResponseModeInternalEnumDTO Parse(string value) => new FrameRecordingSettingsResponseModeInternalEnumDTO(value);

        public override string ToString() => Value;

        public bool Equals(FrameRecordingSettingsResponseModeInternalEnumDTO other) => Value == other.Value;

        public override bool Equals(object obj) => obj is FrameRecordingSettingsResponseModeInternalEnumDTO other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(FrameRecordingSettingsResponseModeInternalEnumDTO left, FrameRecordingSettingsResponseModeInternalEnumDTO right) => left.Equals(right);

        public static bool operator !=(FrameRecordingSettingsResponseModeInternalEnumDTO left, FrameRecordingSettingsResponseModeInternalEnumDTO right) => !left.Equals(right);

        public static implicit operator FrameRecordingSettingsResponseModeInternalEnumDTO(string value) => new FrameRecordingSettingsResponseModeInternalEnumDTO(value);

        public static implicit operator string(FrameRecordingSettingsResponseModeInternalEnumDTO type) => type.Value;
    }
}
