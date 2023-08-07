using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class TargetResolution : IStateLoadableFrom<TargetResolutionInternalDTO, TargetResolution>
    {
        public int Bitrate { get; private set;}

        public int Height { get; private set;}

        public int Width { get; private set;}

        void IStateLoadableFrom<TargetResolutionInternalDTO, TargetResolution>.LoadFromDto(TargetResolutionInternalDTO dto, ICache cache)
        {
            Bitrate = dto.Bitrate;
            Height = dto.Height;
            Width = dto.Width;
        }
    }
}