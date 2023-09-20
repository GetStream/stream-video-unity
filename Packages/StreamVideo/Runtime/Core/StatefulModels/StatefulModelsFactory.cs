using System;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using Cache = StreamVideo.Core.State.Caches.Cache;

namespace StreamVideo.Core.StatefulModels
{
    /// <summary>
    /// Factory for <see cref="IStreamStatefulModel"/>
    /// </summary>
    internal sealed class StatefulModelsFactory : IStatefulModelsFactory
    {
        public StatefulModelsFactory(StreamVideoClient streamChatClient, ISerializer serializer, ILogs logs, Cache cache)
        {
            _streamVideoClient = streamChatClient ?? throw new ArgumentNullException(nameof(streamChatClient));
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            _context = new StatefulModelContext(_cache, streamChatClient, serializer, logs);
        }
        
        public StreamCall CreateStreamCall(string uniqueId) 
            => new StreamCall(uniqueId, _cache.Calls, _context);
        
        public StreamVideoUser CreateStreamVideoUser(string uniqueId) 
            => new StreamVideoUser(uniqueId, _cache.Users, _context);
        
        public StreamVideoCallParticipant CreateStreamVideoCallParticipant(string uniqueId) 
            => new StreamVideoCallParticipant(uniqueId, _cache.CallParticipants, _context);

        // public StreamChannel CreateStreamChannel(string uniqueId)
        //     => new StreamChannel(uniqueId, _cache.Channels, _context);
        //
        // public StreamChannelMember CreateStreamChannelMember(string uniqueId)
        //     => new StreamChannelMember(uniqueId, _cache.ChannelMembers, _context);
        //
        // public StreamLocalUserData CreateStreamLocalUser(string uniqueId)
        //     => new StreamLocalUserData(uniqueId, _cache.LocalUser, _context);
        //
        // public StreamMessage CreateStreamMessage(string uniqueId)
        //     => new StreamMessage(uniqueId, _cache.Messages, _context);
        //
        // public StreamUser CreateStreamUser(string uniqueId)
        //     => new StreamUser(uniqueId, _cache.Users, _context);

        private readonly ILogs _logs;
        private readonly StreamVideoClient _streamVideoClient;
        private readonly IStatefulModelContext _context;
        private readonly ICache _cache;
    }
}