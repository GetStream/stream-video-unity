using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Models
{
   public readonly struct TranscriptionSettingsMode : System.IEquatable<TranscriptionSettingsMode>,
       ILoadableFrom<TranscriptionSettingsModeInternalEnumDTO, TranscriptionSettingsMode>, ISavableTo<TranscriptionSettingsModeInternalEnumDTO>
   {
       public static readonly TranscriptionSettingsMode Available = new TranscriptionSettingsMode("available");
       public static readonly TranscriptionSettingsMode Disabled = new TranscriptionSettingsMode("disabled");
       public static readonly TranscriptionSettingsMode AutoOn = new TranscriptionSettingsMode("auto-on");

       public TranscriptionSettingsMode(string value)
       {
           _value = value;
       }

       public override string ToString() => _value;

       public bool Equals(TranscriptionSettingsMode other) => _value == other._value;

       public override bool Equals(object obj) => obj is TranscriptionSettingsMode other && Equals(other);

       public override int GetHashCode() => _value.GetHashCode();

       public static bool operator ==(TranscriptionSettingsMode left, TranscriptionSettingsMode right) => left.Equals(right);

       public static bool operator !=(TranscriptionSettingsMode left, TranscriptionSettingsMode right) => !left.Equals(right);

       public static implicit operator TranscriptionSettingsMode(string value) => new TranscriptionSettingsMode(value);

       public static implicit operator string(TranscriptionSettingsMode type) => type._value;

       TranscriptionSettingsMode ILoadableFrom<TranscriptionSettingsModeInternalEnumDTO, TranscriptionSettingsMode>.
           LoadFromDto(TranscriptionSettingsModeInternalEnumDTO dto)
           => new TranscriptionSettingsMode(dto.ToString());

       TranscriptionSettingsModeInternalEnumDTO ISavableTo<TranscriptionSettingsModeInternalEnumDTO>.SaveToDto()
           => new TranscriptionSettingsModeInternalEnumDTO(_value);

       private readonly string _value;
   }
}