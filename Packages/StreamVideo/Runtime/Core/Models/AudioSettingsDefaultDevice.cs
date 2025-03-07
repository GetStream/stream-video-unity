using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Models
{
    public readonly struct AudioSettingsDefaultDevice : System.IEquatable<AudioSettingsDefaultDevice>,
        ILoadableFrom<AudioSettingsDefaultDeviceInternalEnumDTO, AudioSettingsDefaultDevice>, ISavableTo<AudioSettingsDefaultDeviceInternalEnumDTO>
    {
        public static readonly AudioSettingsDefaultDevice Speaker = new AudioSettingsDefaultDevice("speaker");
        public static readonly AudioSettingsDefaultDevice Earpiece = new AudioSettingsDefaultDevice("earpiece");

        public AudioSettingsDefaultDevice(string value)
        {
            _value = value;
        }

        public override string ToString() => _value;

        public bool Equals(AudioSettingsDefaultDevice other) => _value == other._value;

        public override bool Equals(object obj) => obj is AudioSettingsDefaultDevice other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(AudioSettingsDefaultDevice left, AudioSettingsDefaultDevice right) => left.Equals(right);

        public static bool operator !=(AudioSettingsDefaultDevice left, AudioSettingsDefaultDevice right) => !left.Equals(right);

        public static implicit operator AudioSettingsDefaultDevice(string value) => new AudioSettingsDefaultDevice(value);

        public static implicit operator string(AudioSettingsDefaultDevice type) => type._value;

        AudioSettingsDefaultDevice ILoadableFrom<AudioSettingsDefaultDeviceInternalEnumDTO, AudioSettingsDefaultDevice>.
            LoadFromDto(AudioSettingsDefaultDeviceInternalEnumDTO dto)
            => new AudioSettingsDefaultDevice(dto.ToString());

        AudioSettingsDefaultDeviceInternalEnumDTO ISavableTo<AudioSettingsDefaultDeviceInternalEnumDTO>.SaveToDto()
            => new AudioSettingsDefaultDeviceInternalEnumDTO(_value);

        private readonly string _value;
    }
}