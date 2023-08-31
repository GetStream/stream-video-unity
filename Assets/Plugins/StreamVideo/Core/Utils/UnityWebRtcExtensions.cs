using System.Text;
using Unity.WebRTC;

namespace StreamVideo.Core.Utils
{
    internal static class UnityWebRtcExtensions
    {
        public static string Print(this RTCIceCandidate iceCandidate)
        {
            _sb.Clear();
            _sb.Append("IceCandidate - ");

            _sb.Append(nameof(iceCandidate.Candidate));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Candidate);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.Address));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Address);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.Component));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Component);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.Foundation));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Foundation);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.Port));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Port);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.Priority));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Priority);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.Protocol));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Protocol);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.Type));
            _sb.Append(": ");
            _sb.Append(iceCandidate.Type);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.RelatedAddress));
            _sb.Append(": ");
            _sb.Append(iceCandidate.RelatedAddress);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.RelatedPort));
            _sb.Append(": ");
            _sb.Append(iceCandidate.RelatedPort);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.UserNameFragment));
            _sb.Append(": ");
            _sb.Append(iceCandidate.UserNameFragment);
            _sb.Append(", ");

            _sb.Append(nameof(iceCandidate.SdpMLineIndex));
            _sb.Append(": ");
            _sb.Append(iceCandidate.SdpMLineIndex);
            _sb.Append(", ");

            return _sb.ToString();
        }

        private static readonly StringBuilder _sb = new StringBuilder();
    }
}