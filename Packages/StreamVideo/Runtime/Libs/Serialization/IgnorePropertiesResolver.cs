using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StreamVideo.Libs.Serialization
{
    internal class IgnorePropertiesResolver : DefaultContractResolver
    {
        public IgnorePropertiesResolver(SerializerIgnorePropertyHandler ignorePropertyHandler)
        {
            _ignorePropertyHandler
                = ignorePropertyHandler ?? throw new ArgumentNullException(nameof(ignorePropertyHandler));
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var shouldIgnore = _ignorePropertyHandler(member);
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
    }
}