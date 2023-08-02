using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core
{
    public interface IStreamCall : IStreamStatefulModel
    {
        
    }
    /// <summary>
    /// Represents a call during which participants can share: audio, video, screen
    /// </summary>
    internal sealed class StreamCall : StreamStatefulModelBase<StreamCall>, IUpdateableFrom<CallResponse, StreamCall>, IStreamCall
    {
        public string UniqueId { get; }
        protected override string InternalUniqueId
        {
            get => Cid;
            set => Cid = value;
        }
        protected override StreamCall Self => this;
        
        public IStreamCallState State { get; } = new StreamCallState();

        public string Cid { get; private set; }
        
        internal StreamCall(string uniqueId, ICacheRepository<StreamCall> repository,
            IStatefulModelContext context)
            : base(uniqueId, repository, context)
        {

        }

        internal void LoadFrom(GetCallResponse dto)
        {
            
        }

        public Task GetOrCreateAsync()
        {
            return Task.CompletedTask;
        }

        /**
   * Will start to watch for call related WebSocket events and initiate a call session with the server.
   *
   * @returns a promise which resolves once the call join-flow has finished.
   */
        public Task JoinAsync()
        {
            return Task.CompletedTask;
        }

        /**
   * Marks the incoming call as accepted.
   *
   * This method should be used only for "ringing" call flows.
   * {@link Call.join} invokes this method automatically for you when joining a call.
   * Unless you are implementing a custom "ringing" flow, you should not use this method.
   */
        public Task AcceptAsync()
        {
            return Task.CompletedTask;
        }

        /**
   * Marks the incoming call as rejected.
   *
   * This method should be used only for "ringing" call flows.
   * {@link Call.leave} invokes this method automatically for you when you leave or reject this call.
   * Unless you are implementing a custom "ringing" flow, you should not use this method.
   */
        public Task Reject()
        {
            return Task.CompletedTask;
        }

        public Task Leave()
        {
            return Task.CompletedTask;
        }
        
        private readonly StreamVideoLowLevelClient _client;
        private readonly StreamCallType _type;
        private string _id;

        public void UpdateFromDto(CallResponse dto, ICache cache)
        {
            var aa = new CallResponse
            {
                Backstage = false,
                BlockedUserIds = null,
                Cid = null,
                CreatedAt = default,
                CreatedBy = null,
                CurrentSessionId = null,
                Custom = null,
                Egress = null,
                EndedAt = default,
                Id = null,
                Ingress = null,
                Recording = false,
                Session = null,
                Settings = null,
                StartsAt = default,
                Team = null,
                Transcribing = false,
                Type = null,
                UpdatedAt = default
            };
        }
    }
}