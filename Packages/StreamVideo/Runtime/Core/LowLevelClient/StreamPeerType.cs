using System;
using StreamVideo.v1.Sfu.Models;

namespace StreamVideo.Core.LowLevelClient
{
    internal enum StreamPeerType
    {
        Publisher,
        Subscriber,
    }

    internal static class StreamPeerTypeExt
    {
        public static PeerType ToPeerType(this StreamPeerType streamPeerType)
        {
            switch (streamPeerType)
            {
                case StreamPeerType.Publisher: return PeerType.PublisherUnspecified;
                case StreamPeerType.Subscriber: return PeerType.Subscriber;
                default:
                    throw new ArgumentOutOfRangeException(nameof(streamPeerType), streamPeerType, null);
            }
        }
        
        public static StreamPeerType ToStreamPeerType(this PeerType peerType)
        {
            switch (peerType)
            {
                case PeerType.PublisherUnspecified: return StreamPeerType.Publisher;
                case PeerType.Subscriber: return StreamPeerType.Subscriber;
                default:
                    throw new ArgumentOutOfRangeException(nameof(peerType), peerType, null);
            }
        }
    }
}