﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace StreamVideo.Core.State
{
    //StreamTodo: fix desc
    /// <summary>
    /// Custom data (max 5KB) that you can assign to:<br/>
    /// - <see cref="IStreamChannel"/><br/>
    /// - <see cref="IStreamMessage"/><br/>
    /// - <see cref="IStreamUser"/><br/>
    /// - <see cref="StreamChannelMember"/><br/>
    /// If you want to have images or files as custom data, upload them using <see cref="IStreamChannel.UploadFileAsync"/> and <see cref="IStreamChannel.UploadImageAsync"/> and put only file URL as a custom data
    /// You can set custom data by using <see cref="IStreamChannel.UpdatePartialAsync"/> or <see cref="IStreamChannel.UpdateOverwriteAsync"/>
    /// </summary>
    /// <remarks>https://getstream.io/chat/docs/unity/creating_channels/?language=unity</remarks>
    public interface IStreamCustomData
    {
        /// <summary>
        /// Count of custom data items
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// All keys of custom data items
        /// </summary>
        IReadOnlyCollection<string> Keys { get; }
        
        /// <summary>
        /// Check whether custom data contains item with specified key
        /// </summary>
        /// <param name="key">Unique key</param>
        bool ContainsKey(string key);
        
        /// <summary>
        /// Get custom data by key and try casting it to <see cref="TType"/>.
        /// </summary>
        /// <param name="key">Unique key of your custom data entry</param>
        /// <typeparam name="TType">Type to which value assigned to this key will be casted. If casting fails the default value for this type will be returned</typeparam>
        TType Get<TType>(string key);

        /// <summary>
        /// Try get custom data by key and try casting it to <see cref="TCast"
        /// </summary>
        /// <param name="key">Unique key of your custom data entry<</param>
        /// <param name="value">Value</param>
        /// <typeparam name="TType">Type to which value assigned to this key will be casted. If casting fails the default value for this type will be returned</typeparam>
        /// <returns>True or False depending whether data for this key is set</returns>
        bool TryGet<TType>(string key, out TType value);

        /// <summary>
        /// Set custom data and sync with the server. This methods makes an API call after setting the data. If you need to set multiple data entries at once you should use <see cref="SetManyAsync"/>.
        /// </summary>
        /// <param name="key">Unique key by witch the custom data entry will be retrieved via <see cref="StreamCustomData.Get{TType}"/></param>
        /// <param name="value">The value. This can be any type, even your custom class but it MUST properly serialize to JSON.</param>
        Task SetAsync(string key, object value);

        /// <summary>
        /// Set multiple entries of custom data and sync with the server. This method is more efficient than <see cref="SetAsync"/> for setting multiple data entries at once because it will set all the data with a single API call.
        /// </summary>
        /// <param name="data"></param>
        Task SetManyAsync(IEnumerable<(string key, object value)> data);
    }
}