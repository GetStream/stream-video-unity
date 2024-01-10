using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.State
{
    /// <inheritdoc cref="IStreamCustomData"/>
    internal class StreamCustomData : IStreamCustomData
    {
        public int Count => _customData.Count;
        public IReadOnlyCollection<string> Keys => _customData.Keys;

        public bool ContainsKey(string key) => _customData.ContainsKey(key);

        public TType Get<TType>(string key) => _serializer.TryConvertTo<TType>(_customData[key]);

        public bool TryGet<TType>(string key, out TType value)
        {
            if (!_customData.ContainsKey(key))
            {
                value = default;
                return false;
            }

            value = _serializer.TryConvertTo<TType>(_customData[key]);
            return true;
        }

        /// <summary>
        /// Set custom data and sync with the server
        /// </summary>
        /// <param name="key">Unique key by witch the custom data entry will be retrieved via <see cref="Get{TType}"/></param>
        /// <param name="value">The value. This can be any type, even your custom class but it MUST properly serialize to JSON.</param>
        public Task SetAsync(string key, object value)
        {
            _customData[key] = value;
            return _customDataServerSyncCallback();
        }
        
        //StreamTodo: Add SetMultipleAsync or SetManyAsync

        internal Dictionary<string, object> InternalDictionary => _customData;

        internal StreamCustomData(ISerializer serializer, Func<Task> customDataServerSyncCallback)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _customDataServerSyncCallback = customDataServerSyncCallback ?? throw new ArgumentNullException(nameof(customDataServerSyncCallback));
        }

        /// <summary>
        /// Replace custom data with the source
        /// </summary>
        internal void ReplaceAllWith(Dictionary<string, object> source)
        {
            _customData.Clear();
            foreach (var keyValuePair in source)
            {
                _customData[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        private readonly Dictionary<string, object> _customData = new Dictionary<string, object>();
        private readonly ISerializer _serializer;
        private readonly Func<Task> _customDataServerSyncCallback;
    }
}