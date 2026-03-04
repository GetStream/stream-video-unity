using System;
using StreamVideo.v1.Sfu.Models;

namespace StreamVideo.Core.LowLevelClient
{
    internal class NegotiationException : Exception
    {
        public ErrorCode SfuErrorCode { get; }

        public NegotiationException(ErrorCode sfuErrorCode)
        {
            SfuErrorCode = sfuErrorCode;
        }
    }
}