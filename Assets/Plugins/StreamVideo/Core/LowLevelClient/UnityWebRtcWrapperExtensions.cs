using System;
using System.Threading.Tasks;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    //StreamTodo: check if can not depend on Unity's WebRTC directly but on an interface only with a lightweight wrapper
    internal static class UnityWebRtcWrapperExtensions
    {
        //StreamTodo: in webRTC example they also check for _peerConnection.SignalingState != RTCSignalingState.Stable
        public static Task<RTCSessionDescription> CreateOfferAsync(this RTCPeerConnection peerConnection) =>
            WaitForOperationAsync(peerConnection.CreateOffer(), r => r.Desc);
        
        public static Task<RTCSessionDescription> CreateOfferAsync(this RTCPeerConnection peerConnection, RTCOfferAnswerOptions options) =>
            WaitForOperationAsync(peerConnection.CreateOffer(ref options), r => r.Desc);

        private static async Task<TResponse> WaitForOperationAsync<TOperation, TResponse>(this TOperation asyncOperation, Func<TOperation, TResponse> response) 
            where TOperation : AsyncOperationBase
        {
            // StreamTodo: refactor to use coroutine runner to reduce runtime allocations
            while (!asyncOperation.IsDone)
            {
                await Task.Delay(1);
            }

            if (asyncOperation.IsError)
            {
                throw new WebRTCException(asyncOperation.Error);
            }

            return response(asyncOperation);
        }
    }
}