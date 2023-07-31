using System;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    internal class WebRtcException : Exception
    {
        public WebRtcException(RTCError error) 
            : base($"Type: {error.errorType}, Message: {error.message}")
        {
        }

    }
}