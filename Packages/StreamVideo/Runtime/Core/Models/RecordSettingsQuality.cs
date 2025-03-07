using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Models
{
   public readonly struct RecordSettingsQuality : System.IEquatable<RecordSettingsQuality>,
       ILoadableFrom<RecordSettingsQualityInternalEnumDTO, RecordSettingsQuality>, ISavableTo<RecordSettingsQualityInternalEnumDTO>
   {
       public static readonly RecordSettingsQuality _360p = new RecordSettingsQuality("360p");
       public static readonly RecordSettingsQuality _480p = new RecordSettingsQuality("480p");
       public static readonly RecordSettingsQuality _720p = new RecordSettingsQuality("720p");
       public static readonly RecordSettingsQuality _1080p = new RecordSettingsQuality("1080p");
       public static readonly RecordSettingsQuality _1440p = new RecordSettingsQuality("1440p");
       public static readonly RecordSettingsQuality Portrait_360x640 = new RecordSettingsQuality("portrait-360x640");
       public static readonly RecordSettingsQuality Portrait_480x854 = new RecordSettingsQuality("portrait-480x854");
       public static readonly RecordSettingsQuality Portrait_720x1280 = new RecordSettingsQuality("portrait-720x1280");
       public static readonly RecordSettingsQuality Portrait_1080x1920 = new RecordSettingsQuality("portrait-1080x1920");
       public static readonly RecordSettingsQuality Portrait_1440x2560 = new RecordSettingsQuality("portrait-1440x2560");

       public RecordSettingsQuality(string value)
       {
           _value = value;
       }

       public override string ToString() => _value;

       public bool Equals(RecordSettingsQuality other) => _value == other._value;

       public override bool Equals(object obj) => obj is RecordSettingsQuality other && Equals(other);

       public override int GetHashCode() => _value.GetHashCode();

       public static bool operator ==(RecordSettingsQuality left, RecordSettingsQuality right) => left.Equals(right);

       public static bool operator !=(RecordSettingsQuality left, RecordSettingsQuality right) => !left.Equals(right);

       public static implicit operator RecordSettingsQuality(string value) => new RecordSettingsQuality(value);

       public static implicit operator string(RecordSettingsQuality type) => type._value;

       RecordSettingsQuality ILoadableFrom<RecordSettingsQualityInternalEnumDTO, RecordSettingsQuality>.
           LoadFromDto(RecordSettingsQualityInternalEnumDTO dto)
           => new RecordSettingsQuality(dto.ToString());

       RecordSettingsQualityInternalEnumDTO ISavableTo<RecordSettingsQualityInternalEnumDTO>.SaveToDto()
           => new RecordSettingsQualityInternalEnumDTO(_value);

       private readonly string _value;
   }
}