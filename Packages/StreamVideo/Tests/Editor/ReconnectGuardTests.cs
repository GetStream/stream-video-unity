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

        [Test]
        public void When_reconnect_triggered_in_joined_state_expect_state_transitions_to_reconnecting()
        {
            var result = _guard.TryBeginReconnection(CallingState.Joined);

            Assert.That(result, Is.True,
                "Reconnection should be approved when CallingState is Joined.");
            Assert.That(_guard.IsReconnecting, Is.True,
                "IsReconnecting should be true after an approved request.");
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
        public void When_reconnect_triggered_in_left_state_expect_request_ignored()
        {
            var result = _guard.TryBeginReconnection(CallingState.Left);

            Assert.That(result, Is.False,
                "Reconnection should be rejected when CallingState is Left.");
            Assert.That(_guard.IsReconnecting, Is.False,
                "IsReconnecting should remain false when the request is rejected.");
        }

        [Test]
        public void When_reconnect_triggered_while_joining_expect_request_ignored()
        {
            var result = _guard.TryBeginReconnection(CallingState.Joining);

            Assert.That(result, Is.False,
                "Reconnection should be rejected when CallingState is Joining.");
            Assert.That(_guard.IsReconnecting, Is.False,
                "IsReconnecting should remain false when the request is rejected.");
        }

        [Test]
        public void When_reconnect_triggered_while_leaving_expect_request_ignored()
        {
            var result = _guard.TryBeginReconnection(CallingState.Leaving);

            Assert.That(result, Is.False,
                "Reconnection should be rejected when CallingState is Leaving.");
            Assert.That(_guard.IsReconnecting, Is.False,
                "IsReconnecting should remain false when the request is rejected.");
        }

        private ReconnectGuard _guard;
    }
}
#endif
