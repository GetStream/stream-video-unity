using System.Collections.Generic;
using StreamVideo.Core.Models;
using StreamVideo.Core.Trace;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.LowLevelClient
{
    internal class SubscriberPeerConnection : PeerConnectionBase
    {
        public SubscriberPeerConnection(ILogs logs, IEnumerable<ICEServer> iceServers, Tracer tracer) 
            : base(logs, StreamPeerType.Subscriber, iceServers, tracer)
        {
            
        }
    }
}