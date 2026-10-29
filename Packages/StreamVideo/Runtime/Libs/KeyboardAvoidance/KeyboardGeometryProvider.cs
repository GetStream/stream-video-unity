using System;

namespace StreamVideo.Libs.KeyboardAvoidance
{
    internal interface IKeyboardGeometryProvider : IDisposable
    {
        KeyboardProviderId Id { get; }
        int Revision { get; }
        string MeasurementFailure { get; }
        void Poll();
        void Reacquire();
        KeyboardGeometrySnapshot Read();
    }

    /// <summary>
    /// Publishes keyboard snapshots only when the measurement changes.
    /// Platform code fills in <see cref="Poll"/>.
    /// </summary>
    internal abstract class KeyboardGeometryProviderBase : IKeyboardGeometryProvider
    {
        public abstract KeyboardProviderId Id { get; }
        public int Revision { get { lock (_gate) return _revision; } }
        public string MeasurementFailure { get { lock (_gate) return _failure; } }

        public abstract void Poll();

        public virtual void Reacquire()
        {
            RequestForce();
            Poll();
        }

        public KeyboardGeometrySnapshot Read()
        {
            lock (_gate)
            {
                return _current;
            }
        }

        public virtual void Dispose()
        {
        }

        protected void RequestForce()
        {
            lock (_gate)
            {
                _force = true;
            }
        }

        protected void Publish(KeyboardGeometrySnapshot snapshot, string failure = null)
        {
            lock (_gate)
            {
                var changed = _force || _revision == 0 || failure != _failure || !snapshot.SameGeometry(_current);
                _failure = failure;
                if (!changed)
                {
                    return;
                }

                _force = false;
                _revision++;
                _current = snapshot.WithRevision(_revision, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d);
            }

            KeyboardAvoidanceDebug.Log("publish",
                KeyboardAvoidanceDebug.Describe(snapshot) + (failure != null ? " failure=" + failure : string.Empty));
        }

        private readonly object _gate = new object();
        private KeyboardGeometrySnapshot _current = KeyboardGeometrySnapshot.Unknown(KeyboardProviderId.None);
        private int _revision;
        private string _failure;
        private bool _force;
    }

    internal static class KeyboardGeometryProviders
    {
        public static IKeyboardGeometryProvider Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidKeyboardGeometryProvider();
#elif UNITY_IOS && !UNITY_EDITOR
            return new IosKeyboardGeometryProvider();
#else
            return new InactiveKeyboardGeometryProvider();
#endif
        }
    }

    /// <summary>
    /// Desktop and the Editor have no software keyboard to avoid.
    /// </summary>
    internal sealed class InactiveKeyboardGeometryProvider : KeyboardGeometryProviderBase
    {
        public override KeyboardProviderId Id => KeyboardProviderId.None;

        public override void Poll()
        {
            // Publish ignores unchanged snapshots, and still advances the revision after Reacquire.
            Publish(KeyboardGeometrySnapshot.Hidden(KeyboardProviderId.None));
        }
    }

    /// <summary>
    /// Feeds measurements from tests without a device keyboard.
    /// </summary>
    internal sealed class SimulatedKeyboardGeometryProvider : KeyboardGeometryProviderBase
    {
        public override KeyboardProviderId Id => KeyboardProviderId.Simulated;

        public void Set(KeyboardGeometrySnapshot snapshot)
        {
            _snapshot = snapshot;
            _hasSnapshot = true;
            Publish(snapshot);
        }

        public override void Poll()
        {
            if (_hasSnapshot)
            {
                Publish(_snapshot);
            }
        }

        private KeyboardGeometrySnapshot _snapshot;
        private bool _hasSnapshot;
    }

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>
    /// Reads the keyboard rectangle Unity already receives from UIKit and clips it to the screen.
    /// A visible keyboard with an empty rectangle stays unknown instead of being treated as zero height.
    /// </summary>
    internal sealed class IosKeyboardGeometryProvider : KeyboardGeometryProviderBase
    {
        public override KeyboardProviderId Id => KeyboardProviderId.Ios;

        public override void Poll()
        {
            if (!UnityEngine.TouchScreenKeyboard.visible)
            {
                Publish(KeyboardGeometrySnapshot.Hidden(KeyboardProviderId.Ios));
                return;
            }

            var area = UnityEngine.TouchScreenKeyboard.area;
            var width = UnityEngine.Device.Screen.width;
            var height = UnityEngine.Device.Screen.height;
            if (width <= 0 || height <= 0 || area.width <= 0f || area.height <= 0f)
            {
                Publish(KeyboardGeometrySnapshot.Unknown(KeyboardProviderId.Ios));
                return;
            }

            if (!KeyboardOcclusion.TryFromBottomLeftScreenRect(area, width, height, out var normalized))
            {
                Publish(KeyboardGeometrySnapshot.Unknown(KeyboardProviderId.Ios));
                return;
            }

            var occlusions = new KeyboardOcclusionList();
            occlusions.Add(normalized);
            Publish(KeyboardGeometrySnapshot.Create(KeyboardVisibility.Visible, true, KeyboardProviderId.Ios, occlusions));
        }
    }
#endif

    /// <summary>
    /// One shared platform reader for every avoidance controller.
    /// </summary>
    internal sealed class KeyboardGeometryService : IDisposable
    {
        public static KeyboardGeometryService Acquire()
        {
            if (_shared == null)
            {
                _shared = new KeyboardGeometryService(KeyboardGeometryProviders.Create(), sharedInstance: true);
            }

            _users++;
            return _shared;
        }

        public static KeyboardGeometryService CreateStandalone(IKeyboardGeometryProvider provider)
            => new KeyboardGeometryService(provider, sharedInstance: false);

        public KeyboardGeometrySnapshot Current => _current;
        public int Revision => _revision;
        public string MeasurementFailure => _provider.MeasurementFailure;

        public void Poll()
        {
            if (_disposed)
            {
                return;
            }

            _provider.Poll();
            var revision = _provider.Revision;
            if (revision == _revision)
            {
                return;
            }

            _revision = revision;
            _current = _provider.Read();
        }

        public void Reacquire()
        {
            if (_disposed)
            {
                return;
            }

            _provider.Reacquire();
            Poll();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_sharedInstance)
            {
                _users = Math.Max(0, _users - 1);
                if (_users > 0)
                {
                    return;
                }

                _shared = null;
            }

            _disposed = true;
            _provider.Dispose();
        }

        private static KeyboardGeometryService _shared;
        private static int _users;

        private readonly IKeyboardGeometryProvider _provider;
        private readonly bool _sharedInstance;
        private KeyboardGeometrySnapshot _current = KeyboardGeometrySnapshot.Unknown(KeyboardProviderId.None);
        private int _revision;
        private bool _disposed;

        private KeyboardGeometryService(IKeyboardGeometryProvider provider, bool sharedInstance)
        {
            _provider = provider;
            _sharedInstance = sharedInstance;
        }
    }
}
