using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using SfuParticipantCount = StreamVideo.v1.Sfu.Models.ParticipantCount;

namespace StreamVideo.Core.Models.Sfu
{
    public class ParticipantCount : IStateLoadableFrom<SfuParticipantCount, ParticipantCount>
    {
        public uint Total { get;private set; }
        public uint Anonymous { get;private set; }
        
        void IStateLoadableFrom<SfuParticipantCount, ParticipantCount>.LoadFromDto(SfuParticipantCount dto, ICache cache)
        {
            Total = dto.Total;
            Anonymous = dto.Anonymous;
        }
    }
}