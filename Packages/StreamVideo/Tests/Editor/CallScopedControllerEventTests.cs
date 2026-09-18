#if STREAM_TESTS_ENABLED
using System;
using NUnit.Framework;
using StreamVideo.Core.BackgroundFilters;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="CallScopedControllerEvent{T}"/>.
    /// </summary>
    internal sealed class CallScopedControllerEventTests
    {
        [Test]
        public void When_handler_added_while_source_missing_expect_it_fires_after_attach()
        {
            var source = new EventSource();
            var forwarder = new CallScopedControllerEvent<int>();
            var count = 0;

            forwarder.Add(_ => count++);

            source.Raise();
            Assert.That(count, Is.EqualTo(0),
                "Handler must be stored, not dropped, when the controller is not yet available.");

            Attach(forwarder, source);
            source.Raise();

            Assert.That(count, Is.EqualTo(1),
                "Subscribe must not silently discard the handler; it must fire once the controller appears.");
        }

        [Test]
        public void When_first_call_detaches_expect_handler_not_invoked_for_second_call()
        {
            var source = new EventSource();
            var callA = new CallScopedControllerEvent<int>();
            var callB = new CallScopedControllerEvent<int>();
            var fromA = 0;
            var fromB = 0;

            callA.Add(_ => fromA++);
            Attach(callA, source);
            source.Raise();
            Assert.That(fromA, Is.EqualTo(1),
                "Call A handler should fire while that call is attached.");

            callA.Detach();

            callB.Add(_ => fromB++);
            Attach(callB, source);
            source.Raise();

            Assert.That(fromA, Is.EqualTo(1),
                "Handlers subscribed on call A must not fire after leave for a later call.");
            Assert.That(fromB, Is.EqualTo(1),
                "Call B handler should fire after it attaches to the shared controller.");
        }

        [Test]
        public void When_added_while_attached_expect_handler_fires()
        {
            var source = new EventSource();
            var forwarder = new CallScopedControllerEvent<int>();
            var count = 0;

            Attach(forwarder, source);
            forwarder.Add(_ => count++);
            source.Raise();

            Assert.That(count, Is.EqualTo(1),
                "A handler added after attach must still be forwarded.");
        }

        [Test]
        public void When_removed_expect_handler_does_not_fire()
        {
            var source = new EventSource();
            var forwarder = new CallScopedControllerEvent<int>();
            var count = 0;
            Action<int> handler = _ => count++;

            forwarder.Add(handler);
            Attach(forwarder, source);
            forwarder.Remove(handler);
            source.Raise();

            Assert.That(count, Is.EqualTo(0),
                "Removed handlers must not fire.");
        }

        private static void Attach(CallScopedControllerEvent<int> forwarder, EventSource source)
        {
            forwarder.Attach(h => source.Raised += h, h => source.Raised -= h);
        }

        private sealed class EventSource
        {
            public event Action<int> Raised;

            public void Raise() => Raised?.Invoke(0);
        }
    }
}
#endif
