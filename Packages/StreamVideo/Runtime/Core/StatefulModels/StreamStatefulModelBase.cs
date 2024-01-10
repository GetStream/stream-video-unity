using System;
using System.Collections.Generic;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// Base class for <see cref="IStreamStatefulModel"/>
    /// </summary>
    /// <typeparam name="TStatefulModel">Type of tracked object</typeparam>
    internal abstract class StreamStatefulModelBase<TStatefulModel> : IStreamStatefulModel
        where TStatefulModel : class, IStreamStatefulModel
    {
        public IStreamCustomData CustomData => _customData;

        string IStreamStatefulModel.UniqueId => InternalUniqueId;

        internal StreamStatefulModelBase(string uniqueId, ICacheRepository<TStatefulModel> repository,
            IStatefulModelContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Client = context.Client ?? throw new ArgumentNullException(nameof(context.Client));
            Logs = context.Logs ?? throw new ArgumentNullException(nameof(context.Logs));
            Cache = context.Cache ?? throw new ArgumentNullException(nameof(context.Cache));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));

            _customData = new StreamCustomData(_additionalProperties, context.Serializer);

            InternalUniqueId = uniqueId;
            Repository.Track(Self);
        }
        
        //StreamTodo: wrap completely the _additionalProperties in StreamCustomData and not operate on both
        protected Dictionary<string, object> GetInternalAdditionalPropertiesDictionary() => _additionalProperties; 

        protected abstract string InternalUniqueId { get; set; }

        protected abstract TStatefulModel Self { get; }
        protected IInternalStreamVideoClient Client { get; }
        protected StreamVideoLowLevelClient LowLevelClient => Client.InternalLowLevelClient;
        protected ILogs Logs { get; }
        protected ICache Cache { get; }
        protected ICacheRepository<TStatefulModel> Repository { get; }

        protected void LoadCustomData(Dictionary<string, object> additionalProperties)
        {
            //StreamTodo: investigate if there's a case we don't want to clear here
            //Without clear channel full update or partial update unset won't work because we'll ignore that WS sent channel without custom data
            
            //StreamTodo: 2, wrap into _customData.Sync(additionalProperties); instead of having a collection here

            //StreamTodo: rename to customData
            _additionalProperties.Clear();
            foreach (var keyValuePair in additionalProperties)
            {
                if (_additionalProperties.ContainsKey(keyValuePair.Key))
                {
                    _additionalProperties[keyValuePair.Key] = keyValuePair.Value;
                    continue;
                }

                _additionalProperties.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        protected static T GetOrDefault<T>(T? source, T defaultValue)
            where T : struct
            => source ?? defaultValue;

        protected static T? GetOrDefault<T>(T? source, T? defaultValue)
            where T : struct
            => source ?? defaultValue;

        protected static T GetOrDefault<T>(T source, T defaultValue)
            where T : class
            => source ?? defaultValue;

        private readonly StreamCustomData _customData;
        private readonly Dictionary<string, object> _additionalProperties = new Dictionary<string, object>();
    }
}