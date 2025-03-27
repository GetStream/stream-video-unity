using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Models
{
   public readonly struct ClosedCaptionMode : System.IEquatable<ClosedCaptionMode>,
       ILoadableFrom<TranscriptionSettingsResponseClosedCaptionModeInternalEnumDTO, ClosedCaptionMode>, ISavableTo<TranscriptionSettingsResponseClosedCaptionModeInternalEnumDTO>
   {
       public static readonly ClosedCaptionMode Available = new ClosedCaptionMode("available");
       public static readonly ClosedCaptionMode Disabled = new ClosedCaptionMode("disabled");
       public static readonly ClosedCaptionMode AutoOn = new ClosedCaptionMode("auto-on");

       public ClosedCaptionMode(string value)
       {
           _value = value;
       }

       public override string ToString() => _value;

       public bool Equals(ClosedCaptionMode other) => _value == other._value;

       public override bool Equals(object obj) => obj is ClosedCaptionMode other && Equals(other);

       public override int GetHashCode() => _value.GetHashCode();

       public static bool operator ==(ClosedCaptionMode left, ClosedCaptionMode right) => left.Equals(right);

       public static bool operator !=(ClosedCaptionMode left, ClosedCaptionMode right) => !left.Equals(right);

       public static implicit operator ClosedCaptionMode(string value) => new ClosedCaptionMode(value);

       public static implicit operator string(ClosedCaptionMode type) => type._value;

       ClosedCaptionMode ILoadableFrom<TranscriptionSettingsResponseClosedCaptionModeInternalEnumDTO, ClosedCaptionMode>.
           LoadFromDto(TranscriptionSettingsResponseClosedCaptionModeInternalEnumDTO dto)
           => new ClosedCaptionMode(dto.ToString());

       TranscriptionSettingsResponseClosedCaptionModeInternalEnumDTO ISavableTo<TranscriptionSettingsResponseClosedCaptionModeInternalEnumDTO>.SaveToDto()
           => new TranscriptionSettingsResponseClosedCaptionModeInternalEnumDTO(_value);

       private readonly string _value;
   }
}