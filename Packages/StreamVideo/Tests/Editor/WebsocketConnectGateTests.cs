#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Libs.Websockets;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="WebsocketConnectGate"/>.
    /// WebGL NativeWebSocket.Connect() returns before the browser socket is Open.
    /// Sending coordinator auth before Open is dropped and the call never starts.
    /// </summary>
    internal sealed class WebsocketConnectGateTests
    {
        [UnityTest]
        public IEnumerator When_already_open_expect_wait_completes_immediately()
            => When_already_open_expect_wait_completes_immediately_Async().RunAsIEnumerator();

        [UnityTest]
        public IEnumerator When_open_event_fires_expect_wait_completes()
            => When_open_event_fires_expect_wait_completes_Async().RunAsIEnumerator();

        [UnityTest]
        public IEnumerator When_error_event_fires_expect_wait_throws()
            => When_error_event_fires_expect_wait_throws_Async().RunAsIEnumerator();

        [UnityTest]
        public IEnumerator When_close_event_fires_expect_wait_throws()
            => When_close_event_fires_expect_wait_throws_Async().RunAsIEnumerator();

        [UnityTest]
        public IEnumerator When_cancelled_expect_wait_throws_task_canceled()
            => When_cancelled_expect_wait_throws_task_canceled_Async().RunAsIEnumerator();

        private async Task When_already_open_expect_wait_completes_immediately_Async()
        {
            var source = new FakeSocket { IsOpen = true };

            await WebsocketConnectGate.WaitUntilOpenAsync(
                isOpen: () => source.IsOpen,
                addOnOpen: handler => source.Opened += handler,
                removeOnOpen: handler => source.Opened -= handler,
                addOnError: handler => source.Errored += handler,
                removeOnError: handler => source.Errored -= handler,
                addOnClose: handler => source.Closed += handler,
                removeOnClose: handler => source.Closed -= handler,
                cancellationToken: CancellationToken.None);

            Assert.That(source.OpenedSubscriberCount, Is.EqualTo(0),
                "Open handler should be unsubscribed after the wait completes.");
        }

        private async Task When_open_event_fires_expect_wait_completes_Async()
        {
            var source = new FakeSocket();
            var wait = WebsocketConnectGate.WaitUntilOpenAsync(
                isOpen: () => source.IsOpen,
                addOnOpen: handler => source.Opened += handler,
                removeOnOpen: handler => source.Opened -= handler,
                addOnError: handler => source.Errored += handler,
                removeOnError: handler => source.Errored -= handler,
                addOnClose: handler => source.Closed += handler,
                removeOnClose: handler => source.Closed -= handler,
                cancellationToken: CancellationToken.None);

            Assert.That(wait.IsCompleted, Is.False,
                "Wait should not complete before the socket reports Open.");

            source.IsOpen = true;
            source.RaiseOpened();
            await wait;

            Assert.That(wait.IsCompleted, Is.True,
                "Wait should complete after the Open event.");
            Assert.That(source.OpenedSubscriberCount, Is.EqualTo(0),
                "Open handler should be unsubscribed after the wait completes.");
        }

        private async Task When_error_event_fires_expect_wait_throws_Async()
        {
            var source = new FakeSocket();
            var wait = WebsocketConnectGate.WaitUntilOpenAsync(
                isOpen: () => source.IsOpen,
                addOnOpen: handler => source.Opened += handler,
                removeOnOpen: handler => source.Opened -= handler,
                addOnError: handler => source.Errored += handler,
                removeOnError: handler => source.Errored -= handler,
                addOnClose: handler => source.Closed += handler,
                removeOnClose: handler => source.Closed -= handler,
                cancellationToken: CancellationToken.None);

            source.RaiseErrored("handshake failed");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => wait);
            Assert.That(exception.Message, Is.EqualTo("handshake failed"),
                "Wait should surface the websocket error message.");
        }

        private async Task When_close_event_fires_expect_wait_throws_Async()
        {
            var source = new FakeSocket();
            var wait = WebsocketConnectGate.WaitUntilOpenAsync(
                isOpen: () => source.IsOpen,
                addOnOpen: handler => source.Opened += handler,
                removeOnOpen: handler => source.Opened -= handler,
                addOnError: handler => source.Errored += handler,
                removeOnError: handler => source.Errored -= handler,
                addOnClose: handler => source.Closed += handler,
                removeOnClose: handler => source.Closed -= handler,
                cancellationToken: CancellationToken.None);

            source.RaiseClosed("Abnormal");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => wait);
            Assert.That(exception.Message, Does.Contain("Abnormal"),
                "Wait should fail when the socket closes during connect.");
        }

        private async Task When_cancelled_expect_wait_throws_task_canceled_Async()
        {
            var source = new FakeSocket();
            using var cts = new CancellationTokenSource();
            var wait = WebsocketConnectGate.WaitUntilOpenAsync(
                isOpen: () => source.IsOpen,
                addOnOpen: handler => source.Opened += handler,
                removeOnOpen: handler => source.Opened -= handler,
                addOnError: handler => source.Errored += handler,
                removeOnError: handler => source.Errored -= handler,
                addOnClose: handler => source.Closed += handler,
                removeOnClose: handler => source.Closed -= handler,
                cancellationToken: cts.Token);

            cts.Cancel();

            try
            {
                await wait;
                Assert.Fail("Wait should throw when the cancellation token is cancelled.");
            }
            catch (Exception exception)
            {
                Assert.That(exception, Is.InstanceOf<OperationCanceledException>(),
                    "Cancelled wait should throw OperationCanceledException.");
            }

            Assert.That(source.OpenedSubscriberCount, Is.EqualTo(0),
                "Handlers should be unsubscribed after cancellation.");
        }

        private sealed class FakeSocket
        {
            public event Action Opened;
            public event Action<string> Errored;
            public event Action<string> Closed;

            public bool IsOpen { get; set; }

            public int OpenedSubscriberCount => Opened == null ? 0 : Opened.GetInvocationList().Length;

            public void RaiseOpened() => Opened?.Invoke();

            public void RaiseErrored(string error) => Errored?.Invoke(error);

            public void RaiseClosed(string reason) => Closed?.Invoke(reason);
        }
    }
}
#endif
