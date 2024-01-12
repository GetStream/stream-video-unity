using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// Base class for <see cref="IStreamStatefulModel"/>
    /// </summary>
    /// <typeparam name="TStatefulModel">Type of tracked object</typeparam>
    internal abstract class StreamStatefulModelBase<TStatefulModel> : IStreamStatefulModel
        where TStatefulModel : class, IStreamStatefulModel
    {
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
            _serializer = context.Serializer ?? throw new ArgumentNullException(nameof(context.Serializer));

            InternalCustomData = new StreamCustomData(context.Serializer, SyncCustomDataAsync);

            InternalUniqueId = uniqueId;
            Repository.Track(Self);
        }

        protected abstract string InternalUniqueId { get; set; }

        protected abstract TStatefulModel Self { get; }
        protected IInternalStreamVideoClient Client { get; }
        protected StreamVideoLowLevelClient LowLevelClient => Client.InternalLowLevelClient;
        protected ILogs Logs { get; }
        protected ICache Cache { get; }
        protected ICacheRepository<TStatefulModel> Repository { get; }
        protected StreamCustomData InternalCustomData { get; }

        protected void LoadCustomData(Dictionary<string, object> customData)
        {
            InternalCustomData.ReplaceAllWith(customData);
        }

        protected abstract Task SyncCustomDataAsync();

        protected static T GetOrDefault<T>(T? source, T defaultValue)
            where T : struct
            => source ?? defaultValue;

        protected static T? GetOrDefault<T>(T? source, T? defaultValue)
            where T : struct
            => source ?? defaultValue;

        protected static T GetOrDefault<T>(T source, T defaultValue)
            where T : class
            => source ?? defaultValue;
        
        private readonly ISerializer _serializer;
    }
}