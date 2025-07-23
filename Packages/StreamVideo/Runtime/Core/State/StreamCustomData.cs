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

            //StreamTOdo: convertion can fail but TryGet will still return true. We either need to return string and let user handle conversion or check if conversion is successful
            // Currently, invalid cast will just return default value for TType. Check how will float conversion to int behave
            value = _serializer.TryConvertTo<TType>(_customData[key]);
            return true;
        }

        public Task SetAsync(string key, object value)
        {
            _customData[key] = value;
            return _customDataServerSyncCallback();
        }
        
        public Task SetManyAsync(IEnumerable<(string key, object value)> data)
        {
            foreach (var (key, value) in data)
            {
                _customData[key] = value;
            }
            return _customDataServerSyncCallback();
        }
        
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