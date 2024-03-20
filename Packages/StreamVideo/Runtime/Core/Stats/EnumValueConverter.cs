using System;
using System.Reflection;
using StreamVideo.Libs.Serialization;
using Unity.WebRTC;

namespace StreamVideo.Core.Stats
{
    internal class EnumValueConverter : ISerializationConverter
    {
        public bool CanConvert(Type objectType) => objectType.IsEnum;

        public object Convert(object value)
        {
            var type = value.GetType();
            var fieldInfo = type.GetField(value.ToString());
            if (fieldInfo == null)
            {
                return value;
            }

            var attribute = (StringValueAttribute)fieldInfo.GetCustomAttribute(typeof(StringValueAttribute));
            return attribute == null ? value : attribute.StringValue;
        }
    }
}