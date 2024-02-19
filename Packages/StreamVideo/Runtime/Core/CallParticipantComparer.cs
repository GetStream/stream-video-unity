using System.Collections.Generic;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core
{
    /// <summary>
    /// User for sorting of <see cref="IStreamCall.SortedParticipants"/>
    /// </summary>
    internal class CallParticipantComparer : IComparer<IStreamVideoCallParticipant>
    {
        public bool OrderChanged { get; private set; }
        
        public int Compare(IStreamVideoCallParticipant x, IStreamVideoCallParticipant y)
        {
            var result = InternalCompare(x, y);
            if (result != 0)
            {
                OrderChanged = true;
            }

            return result;
        }

        public void Reset() => OrderChanged = false;

        private int InternalCompare(IStreamVideoCallParticipant x, IStreamVideoCallParticipant y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x.IsPinned && !y.IsPinned) return -1;
            if (!x.IsPinned && y.IsPinned) return 1;

            if (x.IsScreenSharing && !y.IsScreenSharing) return -1;
            if (!x.IsScreenSharing && y.IsScreenSharing) return 1;

            if (x.IsDominantSpeaker && !y.IsDominantSpeaker) return -1;
            if (!x.IsDominantSpeaker && y.IsDominantSpeaker) return 1;

            if (x.IsVideoEnabled && !y.IsVideoEnabled) return -1;
            if (!x.IsVideoEnabled && y.IsVideoEnabled) return 1;
            
            if (x.IsAudioEnabled && !y.IsAudioEnabled) return -1;
            if (!x.IsAudioEnabled && y.IsAudioEnabled) return 1;

            return x.JoinedAt.CompareTo(y.JoinedAt); // Earlier joiners first
        }
    }
}