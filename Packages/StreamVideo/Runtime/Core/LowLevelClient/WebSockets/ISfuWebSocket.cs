using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Core.Trace;
using StreamVideo.v1.Sfu.Events;
using ICETrickle = StreamVideo.v1.Sfu.Models.ICETrickle;
using Error = StreamVideo.v1.Sfu.Events.Error;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    /// <summary>
    /// Represents the WebSocket connection to the SFU (Selective Forwarding Unit).
    /// Manages the signaling channel used to exchange session descriptions, ICE candidates,
    /// participant events, and health checks between the client and the SFU server.
    /// </summary>
    internal interface ISfuWebSocket : IDisposable
    {
        event Action Connected;
        event Action Disconnected;

        event Action<SubscriberOffer> SubscriberOffer;
        event Action<PublisherAnswer> PublisherAnswer;
        event Action<ConnectionQualityChanged> ConnectionQualityChanged;
        event Action<AudioLevelChanged> AudioLevelChanged;
        event Action<ICETrickle> IceTrickle;
        event Action<ChangePublishQuality> ChangePublishQuality;
        event Action<ParticipantJoined> ParticipantJoined;
        event Action<ParticipantLeft> ParticipantLeft;
        event Action<DominantSpeakerChanged> DominantSpeakerChanged;
        event Action<JoinResponse> JoinResponse;
        event Action<HealthCheckResponse> HealthCheck;
        event Action<TrackPublished> TrackPublished;
        event Action<TrackUnpublished> TrackUnpublished;
        event Action<Error> Error;
        event Action<CallGrantsUpdated> CallGrantsUpdated;
        event Action<GoAway> GoAway;
        event Action<ICERestart> IceRestart;
        event Action<PinsChanged> PinsUpdated;
        event Action CallEnded;
        event Action<ParticipantUpdated> ParticipantUpdated;
        event Action ParticipantMigrationComplete;
        event Action<ChangePublishOptions> ChangePublishOptions;
        event Action<InboundStateNotification> InboundStateNotification;

        bool IsLeaving { get; }
        bool IsClosingClean { get; }
        bool IsHealthy { get; }

        void Update();
        void SetTracer(Tracer tracer);
        void InitNewSession(string sessionId, string sfuUrl, string sfuToken,
            string subscriberOfferSdp, string publisherOfferSdp);
        Task<JoinResponse> ConnectAsync(SfuConnectRequest request, CancellationToken cancellationToken = default);
        Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage);
        void DebugMarkAsOld();
    }
}
