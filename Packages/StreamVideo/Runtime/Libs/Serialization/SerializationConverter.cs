using System;
using Newtonsoft.Json;

namespace StreamVideo.Libs.Serialization
{
    internal class SerializationConverter : JsonConverter
    {
        public SerializationConverter(ISerializationConverter converter)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType) => _converter.CanConvert(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var result = _converter.Convert(value);
            writer.WriteValue(result);
        }

        private readonly ISerializationConverter _converter;
    }
}