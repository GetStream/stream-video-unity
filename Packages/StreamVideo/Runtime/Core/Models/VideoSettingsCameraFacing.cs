using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Models
{
   public readonly struct VideoSettingsCameraFacing : System.IEquatable<VideoSettingsCameraFacing>,
       ILoadableFrom<VideoSettingsCameraFacingInternalEnumDTO, VideoSettingsCameraFacing>, ISavableTo<VideoSettingsCameraFacingInternalEnumDTO>
   {
       public static readonly VideoSettingsCameraFacing Front = new VideoSettingsCameraFacing("front");
       public static readonly VideoSettingsCameraFacing Back = new VideoSettingsCameraFacing("back");
       public static readonly VideoSettingsCameraFacing External = new VideoSettingsCameraFacing("external");

       public VideoSettingsCameraFacing(string value)
       {
           _value = value;
       }

       public override string ToString() => _value;

       public bool Equals(VideoSettingsCameraFacing other) => _value == other._value;

       public override bool Equals(object obj) => obj is VideoSettingsCameraFacing other && Equals(other);

       public override int GetHashCode() => _value.GetHashCode();

       public static bool operator ==(VideoSettingsCameraFacing left, VideoSettingsCameraFacing right) => left.Equals(right);

       public static bool operator !=(VideoSettingsCameraFacing left, VideoSettingsCameraFacing right) => !left.Equals(right);

       public static implicit operator VideoSettingsCameraFacing(string value) => new VideoSettingsCameraFacing(value);

       public static implicit operator string(VideoSettingsCameraFacing type) => type._value;

       VideoSettingsCameraFacing ILoadableFrom<VideoSettingsCameraFacingInternalEnumDTO, VideoSettingsCameraFacing>.
           LoadFromDto(VideoSettingsCameraFacingInternalEnumDTO dto)
           => new VideoSettingsCameraFacing(dto.ToString());

       VideoSettingsCameraFacingInternalEnumDTO ISavableTo<VideoSettingsCameraFacingInternalEnumDTO>.SaveToDto()
           => new VideoSettingsCameraFacingInternalEnumDTO(_value);

       private readonly string _value;
   }
}