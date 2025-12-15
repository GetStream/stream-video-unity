using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.Models;
using StreamVideo.Core.Sfu;
using StreamVideo.Core.Trace;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.v1.Sfu.Signal;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    internal class SubscriberPeerConnection : PeerConnectionBase
    {
        public SubscriberPeerConnection(ILogs logs, IEnumerable<ICEServer> iceServers, Tracer tracer,
            ISerializer serializer, ISfuClient sfuClient)
            : base(logs, StreamPeerType.Subscriber, iceServers, tracer, serializer, sfuClient)
        {
        }

        protected override async Task RestartIce()
        {
            if (SignalingState == RTCSignalingState.HaveRemoteOffer)
            {
                Logs.InfoIfDebug("ICE restart is already in progress");
                return;
            }

            if (PeerConnection.ConnectionState == RTCPeerConnectionState.New)
            {
                Logs.InfoIfDebug("ICE connection is not yet established, skipping restart");
                return;
            }

            var prevIsIceRestarting = IsIceRestarting;
            IsIceRestarting = true;

            try
            {
                var request = new ICERestartRequest
                {
                    PeerType = v1.Sfu.Models.PeerType.Subscriber,
                    SessionId = SfuClient.SessionId,
                };

#if STREAM_DEBUG_ENABLED
                var serializedRequest = Serializer.Serialize(request);
                Logs.Warning($"ICERestartRequest:\n{serializedRequest}");
#endif

                await SfuClient.RpcCallAsync(request, GeneratedAPI.IceRestart, nameof(GeneratedAPI.IceRestart),
                    GetCurrentCancellationTokenOrDefault(), response => response.Error);
            }
            catch (Exception e)
            {
                IsIceRestarting = prevIsIceRestarting;
                throw;
            }
        }
    }
}