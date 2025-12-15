using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.Models;
using StreamVideo.Core.Trace;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.LowLevelClient
{
    internal class SubscriberPeerConnection : PeerConnectionBase
    {
        public SubscriberPeerConnection(ILogs logs, IEnumerable<ICEServer> iceServers, Tracer tracer, ISerializer serializer) 
            : base(logs, StreamPeerType.Subscriber, iceServers, tracer, serializer)
        {
            
        }

        protected override Task RestartIce()
        {
            throw new System.NotImplementedException();
        }
    }
}