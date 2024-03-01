using System;

namespace StreamVideo.Core.LowLevelClient
{
    internal class DisposedDuringOperationException : Exception
    {
        
    }

    internal static class AppDisposedDuringOperationExceptionExt
    {
        public static void ThrowDisposedDuringOperationIfNull(this StreamPeerConnection streamPeerConnection)
        {
            if (streamPeerConnection == null)
            {
                throw new DisposedDuringOperationException();
            }
        }
    }
}