using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StreamVideo.Libs.Serialization
{
    /// <summary>
    /// https://www.newtonsoft.com/ Json implementation of <see cref="ISerializer"/>
    /// </summary>
    public class NewtonsoftJsonSerializer : ISerializer
    {
        public string Serialize<TType>(TType obj)
            => JsonConvert.SerializeObject(obj, Formatting.None, _defaultSettings);

        public string Serialize<TType>(TType obj, ISerializationOptions serializationOptions)
        {
            var settings = CreateCustomSettings(serializationOptions);
            return JsonConvert.SerializeObject(obj, Formatting.None, settings);
        }

        private JsonSerializerSettings CreateCustomSettings(ISerializationOptions serializationOptions)
        {
            var settings = new JsonSerializerSettings(_defaultSettings);

            if (serializationOptions.IgnorePropertyHandler != null || serializationOptions.NamingStrategy != null)
            {
                settings.ContractResolver = new JsonContractResolver(serializationOptions);
            }

            if (serializationOptions.Converters != null)
            {
                var jsonConverters = new List<JsonConverter>(serializationOptions.Converters.Length);

                foreach (var sourceConverter in serializationOptions.Converters)
                {
                    var converter = new SerializationConverter(sourceConverter);
                    jsonConverters.Add(converter);
                }

                settings.Converters = jsonConverters;

            }

            return settings;
        }

        public TType Deserialize<TType>(string serializedObj) => JsonConvert.DeserializeObject<TType>(serializedObj);

        public TTargetType TryConvertTo<TTargetType>(object serializedObj)
        {
            if (serializedObj is JObject jObject)
            {
                return jObject.ToObject<TTargetType>();
            }

            if (serializedObj is JArray jArray)
            {
                return jArray.ToObject<TTargetType>();
            }

            if (serializedObj is JToken jToken)
            {
                return jToken.ToObject<TTargetType>();
            }

            if (serializedObj is TTargetType targetType)
            {
                return targetType;
            }

            try
            {
                return (TTargetType)Convert.ChangeType(serializedObj, typeof(TTargetType));
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        public object DeserializeObject(string serializedObj) => JsonConvert.DeserializeObject(serializedObj);

        public bool TryPeekValue<TValue>(string serializedObj, string key, out TValue value)
        {
            var wrapperJObject = JObject.Parse(serializedObj);
            if (!wrapperJObject.ContainsKey(key))
            {
                value = default;
                return false;
            }

            var obj = wrapperJObject[key];

            if (obj is JObject childJObject)
            {
                value = childJObject.ToObject<TValue>();
                return true;
            }

            if (obj is JToken childToken)
            {
                value = childToken.Value<TValue>();
                return true;
            }

            throw new Exception("Unhandled object type: " + obj.GetType());
        }
        
        private readonly JsonSerializerSettings _defaultSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };
    }
}