using System;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    internal class WebRTCException : Exception
    {
        public WebRTCException(RTCError error) 
            : base($"Type: {error.errorType}, Message: {error.message}")
        {
        }

    }
}