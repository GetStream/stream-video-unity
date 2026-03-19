#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="SessionID"/> — the foundation of stale-operation detection.
    /// A bug in SessionID would break all version guards used to invalidate stale ICE restarts
    /// and other operations that were queued before a rejoin completed.
    /// </summary>
    internal sealed class SessionIDTests
    {
        [SetUp]
        public void SetUp()
        {
            _sessionId = new SessionID();
        }

        [Test]
        public void When_new_instance_expect_empty()
        {
            Assert.That(_sessionId.IsEmpty, Is.True,
                "A freshly created SessionID should be empty.");
        }

        [Test]
        public void When_new_instance_expect_version_zero()
        {
            Assert.That(_sessionId.Version, Is.EqualTo(0),
                "A freshly created SessionID should have Version == 0.");
        }

        [Test]
        public void When_new_instance_expect_to_string_returns_empty()
        {
            Assert.That(_sessionId.ToString(), Is.EqualTo(string.Empty),
                "A freshly created SessionID should return an empty string from ToString().");
        }

        [Test]
        public void When_regenerate_expect_non_empty_id()
        {
            _sessionId.Regenerate();

            Assert.That(_sessionId.IsEmpty, Is.False,
                "After Regenerate(), IsEmpty should be false.");
            Assert.That(_sessionId.ToString(), Is.Not.Empty,
                "After Regenerate(), ToString() should return a non-empty string.");
        }

        [Test]
        public void When_regenerate_multiple_times_expect_version_increments()
        {
            Assert.That(_sessionId.Version, Is.EqualTo(0),
                "Precondition: new instance should have Version 0 before Regenerate().");

            _sessionId.Regenerate();
            Assert.That(_sessionId.Version, Is.EqualTo(1),
                "First Regenerate() should set Version to 1.");

            _sessionId.Regenerate();
            Assert.That(_sessionId.Version, Is.EqualTo(2),
                "Second Regenerate() should set Version to 2.");

            _sessionId.Regenerate();
            Assert.That(_sessionId.Version, Is.EqualTo(3),
                "Third Regenerate() should set Version to 3.");
        }

        [Test]
        public void When_regenerate_twice_expect_different_ids()
        {
            _sessionId.Regenerate();
            var firstId = _sessionId.ToString();

            _sessionId.Regenerate();
            var secondId = _sessionId.ToString();

            Assert.That(secondId, Is.Not.EqualTo(firstId),
                "Each call to Regenerate() should produce a different ID (new GUID).");
        }

        [Test]
        public void When_regenerate_many_times_expect_all_unique_ids()
        {
            const int iterations = 100;
            var ids = new System.Collections.Generic.HashSet<string>();

            for (var i = 0; i < iterations; i++)
            {
                _sessionId.Regenerate();
                var id = _sessionId.ToString();
                Assert.That(ids.Add(id), Is.True,
                    $"Regenerate() produced a duplicate ID '{id}' on iteration {i + 1}.");
            }

            Assert.That(_sessionId.Version, Is.EqualTo(iterations),
                $"After {iterations} calls to Regenerate(), Version should be {iterations}.");
        }

        [Test]
        public void When_regenerate_expect_valid_guid_format()
        {
            _sessionId.Regenerate();
            var id = _sessionId.ToString();

            Assert.That(System.Guid.TryParse(id, out _), Is.True,
                $"Regenerate() should produce a valid GUID string, but got '{id}'.");
        }

        [Test]
        public void When_clear_after_regenerate_expect_empty()
        {
            _sessionId.Regenerate();
            Assert.That(_sessionId.IsEmpty, Is.False, "Precondition: should not be empty after Regenerate().");

            _sessionId.Clear();

            Assert.That(_sessionId.IsEmpty, Is.True,
                "After Clear(), IsEmpty should be true.");
            Assert.That(_sessionId.ToString(), Is.EqualTo(string.Empty),
                "After Clear(), ToString() should return an empty string.");
        }

        [Test]
        public void When_clear_expect_version_unchanged()
        {
            _sessionId.Regenerate();
            _sessionId.Regenerate();
            Assert.That(_sessionId.Version, Is.EqualTo(2), "Precondition: Version should be 2 after two Regenerate() calls.");

            _sessionId.Clear();

            Assert.That(_sessionId.Version, Is.EqualTo(2),
                "Clear() should not reset the Version counter. Version tracks the number of regenerations, not the current session state.");
        }

        [Test]
        public void When_regenerate_after_clear_expect_non_empty_and_version_incremented()
        {
            _sessionId.Regenerate();
            var versionBeforeClear = _sessionId.Version;

            _sessionId.Clear();
            Assert.That(_sessionId.IsEmpty, Is.True, "Precondition: should be empty after Clear().");

            _sessionId.Regenerate();

            Assert.That(_sessionId.IsEmpty, Is.False,
                "After Clear() followed by Regenerate(), IsEmpty should be false.");
            Assert.That(_sessionId.Version, Is.EqualTo(versionBeforeClear + 1),
                "Regenerate() after Clear() should continue incrementing the Version counter.");
        }

        private SessionID _sessionId;
    }
}
#endif
