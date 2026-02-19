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

        public override async Task RestartIce()
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
            
            if (SfuClient.CallState == CallingState.Reconnecting || SfuClient.CallState == CallingState.Joining)
            {
                Logs.InfoIfDebug($"[{PeerType}] Skipping ICE restart because CallState is {SfuClient.CallState}");
                return;
            }

            var prevIsIceRestarting = IsIceRestarting;
            IsIceRestarting = true;
            
            var sessionVersionAtStart = SfuClient.SessionVersion;

            try
            {
                var request = new ICERestartRequest
                {
                    PeerType = v1.Sfu.Models.PeerType.Subscriber,
                    SessionId = SfuClient.SessionId.ToString(),
                };

#if STREAM_DEBUG_ENABLED
                var serializedRequest = Serializer.Serialize(request);
                Logs.Warning($"ICERestartRequest:\n{serializedRequest}");
#endif

                var result = await SfuClient.RpcCallAsync(request, GeneratedAPI.IceRestart, nameof(GeneratedAPI.IceRestart),
                    GetCurrentCancellationTokenOrDefault(), response => response.Error);
                
                if (SfuClient.SessionVersion != sessionVersionAtStart)
                {
                    Logs.InfoIfDebug($"[{PeerType}] ICE restart result is stale - session version changed from {sessionVersionAtStart} to {SfuClient.SessionVersion}");
                    IsIceRestarting = false;
                    return;
                }
                
                if (result.Error != null)
                {
                    throw new NegotiationException(result.Error.Code);
                }
            }
            catch (Exception e)
            {
                IsIceRestarting = prevIsIceRestarting;
                throw;
            }
        }
    }
}