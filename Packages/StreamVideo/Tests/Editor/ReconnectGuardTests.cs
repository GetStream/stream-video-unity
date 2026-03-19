#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="ReconnectGuard"/> — the guard logic that prevents
    /// reconnection in states where it would be unsafe or redundant.
    /// A bug here could cause parallel reconnect attempts, corrupt an in-progress
    /// join, or resurrect a call after the user has left.
    /// </summary>
    internal sealed class ReconnectGuardTests
    {
        [SetUp]
        public void SetUp()
        {
            _guard = new ReconnectGuard();
        }

        [TestCase(CallingState.Joined)]
        [TestCase(CallingState.Offline)]
        public void When_reconnect_triggered_in_allowed_state_expect_request_approved(CallingState state)
        {
            var result = _guard.TryBeginReconnection(state);

            Assert.That(result, Is.True,
                $"Reconnection should be approved when CallingState is {state}.");
            Assert.That(_guard.IsReconnecting, Is.True,
                "IsReconnecting should be true after an approved request.");
        }

        [TestCase(CallingState.Left)]
        [TestCase(CallingState.Joining)]
        [TestCase(CallingState.Leaving)]
        [TestCase(CallingState.Reconnecting)]
        [TestCase(CallingState.Migrating)]
        [TestCase(CallingState.ReconnectingFailed)]
        public void When_reconnect_triggered_in_ignored_state_expect_request_rejected(CallingState state)
        {
            var result = _guard.TryBeginReconnection(state);

            Assert.That(result, Is.False,
                $"Reconnection should be rejected when CallingState is {state}.");
            Assert.That(_guard.IsReconnecting, Is.False,
                "IsReconnecting should remain false when the request is rejected.");
        }

        [Test]
        public void When_reconnect_triggered_while_already_reconnecting_expect_request_ignored()
        {
            _guard.TryBeginReconnection(CallingState.Joined);

            var secondResult = _guard.TryBeginReconnection(CallingState.Joined);

            Assert.That(secondResult, Is.False,
                "A second reconnection request should be rejected while one is already in progress.");
            Assert.That(_guard.IsReconnecting, Is.True,
                "IsReconnecting should remain true from the first request.");
        }

        [Test]
        public void When_end_reconnection_called_expect_is_reconnecting_resets_and_new_attempt_allowed()
        {
            _guard.TryBeginReconnection(CallingState.Joined);
            Assert.That(_guard.IsReconnecting, Is.True,
                "Precondition: IsReconnecting should be true after a successful begin.");

            _guard.EndReconnection();

            Assert.That(_guard.IsReconnecting, Is.False,
                "EndReconnection should reset IsReconnecting to false.");

            var secondResult = _guard.TryBeginReconnection(CallingState.Joined);

            Assert.That(secondResult, Is.True,
                "After EndReconnection, a new reconnection request should be approved.");
            Assert.That(_guard.IsReconnecting, Is.True,
                "IsReconnecting should be true again after the second approved request.");
        }

        private ReconnectGuard _guard;
    }
}
#endif
