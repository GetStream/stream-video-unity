using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    //StreamTodo: check if can not depend on Unity's WebRTC directly but on an interface only with a lightweight wrapper
    internal static class UnityWebRtcWrapperExtensions
    {
        //StreamTodo: in webRTC example they also check for _peerConnection.SignalingState != RTCSignalingState.Stable
        public static Task<RTCSessionDescription> CreateOfferAsync(this RTCPeerConnection peerConnection, CancellationToken cancellationToken)
            => WaitForOperationAsync(peerConnection.CreateOffer(), r => r.Desc, cancellationToken);

        public static Task<RTCSessionDescription> CreateOfferAsync(this RTCPeerConnection peerConnection,
            ref RTCOfferAnswerOptions options, CancellationToken cancellationToken)
            => WaitForOperationAsync(peerConnection.CreateOffer(ref options), r => r.Desc, cancellationToken);

        public static Task<RTCSessionDescription> CreateAnswerAsync(this RTCPeerConnection peerConnection, CancellationToken cancellationToken)
            => WaitForOperationAsync(peerConnection.CreateAnswer(), r => r.Desc, cancellationToken);

        public static Task SetLocalDescriptionAsync(this RTCPeerConnection peerConnection,
            ref RTCSessionDescription desc, CancellationToken cancellationToken)
            => WaitForOperationAsync(peerConnection.SetLocalDescription(ref desc), cancellationToken);

        public static Task SetRemoteDescriptionAsync(this RTCPeerConnection peerConnection,
            ref RTCSessionDescription desc, CancellationToken cancellationToken)
            => WaitForOperationAsync(peerConnection.SetRemoteDescription(ref desc), cancellationToken);

        public static Task<RTCStatsReport> GetStatsAsync(this RTCPeerConnection peerConnection, CancellationToken cancellationToken)
            => WaitForOperationAsync(peerConnection.GetStats(), r => r.Value, cancellationToken);

        private static async Task<TResponse> WaitForOperationAsync<TOperation, TResponse>(
            this TOperation asyncOperation, Func<TOperation, TResponse> response, CancellationToken cancellationToken)
            where TOperation : AsyncOperationBase
        {
            // StreamTodo: refactor to use coroutine runner to reduce runtime allocations
            while (!asyncOperation.IsDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
            }

            if (asyncOperation.IsError)
            {
                throw new WebRtcException(asyncOperation.Error);
            }

            return response(asyncOperation);
        }

        private static async Task WaitForOperationAsync<TOperation>(
            this TOperation asyncOperation, CancellationToken cancellationToken)
            where TOperation : AsyncOperationBase
        {
            // StreamTodo: refactor to use coroutine runner to reduce runtime allocations
            while (!asyncOperation.IsDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
            }

            if (asyncOperation.IsError)
            {
                throw new WebRtcException(asyncOperation.Error);
            }
        }
    }
}