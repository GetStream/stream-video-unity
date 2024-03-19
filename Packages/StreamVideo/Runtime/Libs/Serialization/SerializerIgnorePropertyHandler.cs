using System.Reflection;

namespace StreamVideo.Libs.Serialization
{
    public delegate bool SerializerIgnorePropertyHandler(MemberInfo propertyInfo);
}