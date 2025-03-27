using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Models
{
    public readonly struct RecordSettingsMode : System.IEquatable<RecordSettingsMode>,
        ILoadableFrom<RecordSettingsModeInternalEnumDTO, RecordSettingsMode>, ISavableTo<RecordSettingsModeInternalEnumDTO>
    {
        public static readonly RecordSettingsMode Available = new RecordSettingsMode("available");
        public static readonly RecordSettingsMode Disabled = new RecordSettingsMode("disabled");
        public static readonly RecordSettingsMode AutoOn = new RecordSettingsMode("auto-on");

        public RecordSettingsMode(string value)
        {
            _value = value;
        }

        public override string ToString() => _value;

        public bool Equals(RecordSettingsMode other) => _value == other._value;

        public override bool Equals(object obj) => obj is RecordSettingsMode other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(RecordSettingsMode left, RecordSettingsMode right) => left.Equals(right);

        public static bool operator !=(RecordSettingsMode left, RecordSettingsMode right) => !left.Equals(right);

        public static implicit operator RecordSettingsMode(string value) => new RecordSettingsMode(value);

        public static implicit operator string(RecordSettingsMode type) => type._value;

        RecordSettingsMode ILoadableFrom<RecordSettingsModeInternalEnumDTO, RecordSettingsMode>.
            LoadFromDto(RecordSettingsModeInternalEnumDTO dto)
            => new RecordSettingsMode(dto.ToString());

        RecordSettingsModeInternalEnumDTO ISavableTo<RecordSettingsModeInternalEnumDTO>.SaveToDto()
            => new RecordSettingsModeInternalEnumDTO(_value);

        private readonly string _value;
    }
}