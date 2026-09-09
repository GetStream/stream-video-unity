using System;
using System.Threading;
using System.Threading.Tasks;

namespace StreamVideo.Libs.Websockets
{
    /// <summary>
    /// Completes when a websocket reaches Open, or fails if it errors/closes first.
    /// NativeWebSocket's WebGL <c>Connect()</c> returns before the browser handshake finishes.
    /// </summary>
    internal static class WebsocketConnectGate
    {
        public static async Task WaitUntilOpenAsync(
            Func<bool> isOpen,
            Action<Action> addOnOpen,
            Action<Action> removeOnOpen,
            Action<Action<string>> addOnError,
            Action<Action<string>> removeOnError,
            Action<Action<string>> addOnClose,
            Action<Action<string>> removeOnClose,
            CancellationToken cancellationToken)
        {
            if (isOpen == null)
            {
                throw new ArgumentNullException(nameof(isOpen));
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void HandleOpen() => tcs.TrySetResult(true);

            void HandleError(string error)
            {
                var message = string.IsNullOrEmpty(error)
                    ? "WebSocket error during connect."
                    : error;
                tcs.TrySetException(new InvalidOperationException(message));
            }

            void HandleClose(string reason)
            {
                tcs.TrySetException(
                    new InvalidOperationException($"WebSocket closed during connect: {reason}"));
            }

            addOnOpen(HandleOpen);
            addOnError(HandleError);
            addOnClose(HandleClose);

            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                try
                {
                    if (isOpen())
                    {
                        tcs.TrySetResult(true);
                    }

                    await tcs.Task;
                }
                finally
                {
                    removeOnOpen(HandleOpen);
                    removeOnError(HandleError);
                    removeOnClose(HandleClose);
                }
            }
        }
    }
}
