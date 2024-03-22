using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StreamVideo.Libs.Serialization
{
    internal class JsonContractResolver : DefaultContractResolver
    {
        public JsonContractResolver(ISerializationOptions options)
        {
            if (options.IgnorePropertyHandler != null)
            {
                _ignorePropertyHandler = options.IgnorePropertyHandler;
            }

            if (options.NamingStrategy.HasValue)
            {
                NamingStrategy = getNamingStrategy(options.NamingStrategy.Value);
            }
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var shouldIgnore = _ignorePropertyHandler != null && _ignorePropertyHandler(member);
            if (shouldIgnore)
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }

#if STREAM_DEBUG_ENABLED
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            return base.CreateProperties(type, memberSerialization)
                .OrderBy(p => p.PropertyName)
                .ToList();
        }
#endif

        private readonly SerializerIgnorePropertyHandler _ignorePropertyHandler;
        
        private NamingStrategy getNamingStrategy(SerializationNamingStrategy serializationNamingStrategy)
        {
            switch (serializationNamingStrategy)
            {
                case SerializationNamingStrategy.Default: return new DefaultNamingStrategy();
                case SerializationNamingStrategy.CamelCase: return new CamelCaseNamingStrategy();
                case SerializationNamingStrategy.KebabCase: return new KebabCaseNamingStrategy();
                case SerializationNamingStrategy.SnakeCase: return new SnakeCaseNamingStrategy();
                default:
                    throw new ArgumentOutOfRangeException(nameof(serializationNamingStrategy), serializationNamingStrategy, null);
            }
        }
    }
}