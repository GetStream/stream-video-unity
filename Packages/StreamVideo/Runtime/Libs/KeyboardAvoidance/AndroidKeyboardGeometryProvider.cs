#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace StreamVideo.Libs.KeyboardAvoidance
{
    /// <summary>
    /// Measures the Android keyboard with window insets and clips it to the UI Toolkit screen.
    /// The native view can shrink while the panel is still laid out at the previous screen height.
    /// Clipping only to that shrunk view would report almost no overlap and leave the field covered.
    /// The listener is attached to a dedicated empty view so it does not replace the activity's own inset listener.
    /// </summary>
    internal sealed class AndroidKeyboardGeometryProvider : KeyboardGeometryProviderBase
    {
        public override KeyboardProviderId Id => KeyboardProviderId.Android;

        public override void Poll()
        {
            if (_permanentFailure)
            {
                return;
            }

            EnsureStarted();
            if (_sdk >= 30)
            {
                PublishModern();
            }
            else if (_sdk > 0)
            {
                PublishLegacy();
            }
        }

        public override void Reacquire()
        {
            RequestForce();
            Poll();
            RequestInsets();
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            var activity = _activity;
            var observer = _observer;
            var host = _host;
            var decor = _decor;
            var frame = _frame;
            _observer = null;
            _listener = null;
            _host = null;
            _decor = null;
            _frame = null;
            _activity = null;

            if (activity == null)
            {
                observer?.Dispose();
                host?.Dispose();
                decor?.Dispose();
                frame?.Dispose();
                return;
            }

            try
            {
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        observer?.Call("setOnApplyWindowInsetsListener", null);
                        if (host != null && observer != null)
                        {
                            host.Call("removeView", observer);
                        }
                    }
                    catch (Exception)
                    {
                        // The activity is already tearing down.
                    }

                    observer?.Dispose();
                    host?.Dispose();
                    decor?.Dispose();
                    frame?.Dispose();
                    activity.Dispose();
                }));
            }
            catch (Exception)
            {
                observer?.Dispose();
                host?.Dispose();
                decor?.Dispose();
                frame?.Dispose();
                activity.Dispose();
            }
        }

        private void EnsureStarted()
        {
            if (_started || _startQueued || _disposed || _permanentFailure)
            {
                return;
            }

            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null)
                {
                    return;
                }

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _sdk = version.GetStatic<int>("SDK_INT");
                _activity = activity;
                _startQueued = true;
                _listener = new InsetsListener(this);
                activity.Call("runOnUiThread", new AndroidJavaRunnable(StartOnUiThread));
                PinListener();
            }
            catch (Exception exception)
            {
                Fail("Keyboard avoidance cannot read the Android keyboard: " + exception.Message, permanent: true);
            }
        }

        private void StartOnUiThread()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                using var window = _activity.Call<AndroidJavaObject>("getWindow");
                _decor = window?.Call<AndroidJavaObject>("getDecorView");
                _host = FindHost(_activity);
                if (_host == null || _decor == null)
                {
                    _startQueued = false;
                    return;
                }

                _frame = new AndroidJavaObject("android.graphics.Rect");
                if (_sdk >= 30)
                {
                    using var type = new AndroidJavaClass("android.view.WindowInsets$Type");
                    _imeType = type.CallStatic<int>("ime");
                }

                _observer = new AndroidJavaObject("android.view.View", _activity);
                using var layout = new AndroidJavaObject("android.view.ViewGroup$LayoutParams", 0, 0);
                _host.Call("addView", _observer, layout);
                _observer.Call("setClickable", false);
                _observer.Call("setFocusable", false);
                _observer.Call("setOnApplyWindowInsetsListener", _listener);
                _observer.Call("requestApplyInsets");
                _started = true;
                if (_sdk < 30)
                {
                    MeasureLegacyGap();
                }
            }
            catch (Exception exception)
            {
                _startQueued = false;
                Fail("Keyboard avoidance cannot read the Android keyboard: " + exception.Message, permanent: true);
            }
        }

        private void OnInsets(AndroidJavaObject insets)
        {
            if (_disposed)
            {
                return;
            }

            AndroidJavaObject root = null;
            try
            {
                root = _decor?.Call<AndroidJavaObject>("getRootWindowInsets");
                if (_sdk >= 30)
                {
                    MeasureIme(root ?? insets);
                }
                else
                {
                    MeasureLegacyGap();
                }
            }
            catch (Exception exception)
            {
                Fail("Keyboard avoidance cannot read the Android keyboard: " + exception.Message, permanent: false);
            }
            finally
            {
                root?.Dispose();
            }
        }

        private void MeasureIme(AndroidJavaObject insets)
        {
            if (insets == null || _decor == null || _frame == null)
            {
                return;
            }

            var visible = insets.Call<bool>("isVisible", _imeType);
            if (!TryReadView(out var viewX, out var viewY, out var viewW, out var viewH))
            {
                return;
            }

            _decor.Call("getWindowVisibleDisplayFrame", _frame);
            var frameBottom = _frame.Get<int>("bottom");
            _decor.Call("getLocationOnScreen", _location);
            var decorX = _location[0];
            var decorY = _location[1];
            var bounds = new int[16];
            var count = 0;

            if (visible && _sdk >= 31)
            {
                using var rects = insets.Call<AndroidJavaObject>("getBoundingRects", _imeType);
                if (rects != null)
                {
                    var rectCount = rects.Call<int>("size");
                    for (var i = 0; i < rectCount && count < 4; i++)
                    {
                        using var rect = rects.Call<AndroidJavaObject>("get", i);
                        if (rect == null)
                        {
                            continue;
                        }

                        var offset = count * 4;
                        bounds[offset] = decorX + rect.Get<int>("left");
                        bounds[offset + 1] = decorY + rect.Get<int>("top");
                        bounds[offset + 2] = decorX + rect.Get<int>("right");
                        bounds[offset + 3] = decorY + rect.Get<int>("bottom");
                        count++;
                    }
                }
            }
            else if (visible)
            {
                using var ime = insets.Call<AndroidJavaObject>("getInsets", _imeType);
                var leftInset = ime.Get<int>("left");
                var rightInset = ime.Get<int>("right");
                if (leftInset != 0 || rightInset != 0)
                {
                    // Edge insets cannot describe a partial-width keyboard. API 31 reports real rectangles.
                    Fail("Keyboard avoidance cannot represent this Android keyboard shape, so the input was left where it is.",
                        permanent: false);
                    return;
                }

                var decorW = _decor.Call<int>("getWidth");
                var decorH = _decor.Call<int>("getHeight");
                var bottomInset = ime.Get<int>("bottom");
                var topInset = ime.Get<int>("top");
                if (bottomInset > 0)
                {
                    bounds[0] = decorX;
                    bounds[1] = decorY + decorH - bottomInset;
                    bounds[2] = decorX + decorW;
                    bounds[3] = decorY + decorH;
                    count++;
                }

                if (topInset > 0)
                {
                    var offset = count * 4;
                    bounds[offset] = decorX;
                    bounds[offset + 1] = decorY;
                    bounds[offset + 2] = decorX + decorW;
                    bounds[offset + 3] = decorY + topInset;
                    count++;
                }
            }

            KeyboardAvoidanceDebug.Log("android.measure",
                "sdk=" + _sdk + " imeVisible=" + visible
                + " decor=(" + decorX + "," + decorY + " " + _decor.Call<int>("getWidth") + "x" + _decor.Call<int>("getHeight") + ")"
                + " host=(" + viewX + "," + viewY + " " + viewW + "x" + viewH + ")"
                + " visibleFrame=[top=" + _frame.Get<int>("top") + " bottom=" + frameBottom + "]"
                + " imeRects=" + KeyboardAvoidanceDebug.DescribePixels(bounds, count));

            lock (_legacyGate)
            {
                _imeVisible = visible;
                _rawViewX = viewX;
                _rawViewY = viewY;
                _rawViewW = viewW;
                _rawViewH = viewH;
                _rawFrameBottom = frameBottom;
                _imeCount = count;
                for (var i = 0; i < count * 4; i++)
                {
                    _imeBounds[i] = bounds[i];
                }

                _rawValid = true;
            }
        }

        private void PublishModern()
        {
            bool valid;
            bool imeVisible;
            int viewX;
            int viewY;
            int viewH;
            int frameBottom;
            int imeCount;
            int resting;
            lock (_legacyGate)
            {
                valid = _rawValid;
                imeVisible = _imeVisible;
                viewX = _rawViewX;
                viewY = _rawViewY;
                viewH = _rawViewH;
                frameBottom = _rawFrameBottom;
                imeCount = _imeCount;
                resting = _restingCovered;
                Array.Copy(_imeBounds, _imeCopy, _imeCopy.Length);
            }

            KeyboardAvoidanceDebug.Log("android.state",
                "valid=" + valid + " imeVisible=" + imeVisible
                + " tskVisible=" + TouchScreenKeyboard.visible + " tskHideInput=" + TouchScreenKeyboard.hideInput
                + " screen=" + Screen.width + "x" + Screen.height
                + " host=(" + viewX + "," + viewY + " h=" + viewH + ")"
                + " visibleFrameBottom=" + frameBottom + " resting=" + resting
                + " imeRects=" + KeyboardAvoidanceDebug.DescribePixels(_imeCopy, imeCount));

            if (!valid)
            {
                return;
            }

            var screenW = Screen.width;
            var screenH = Screen.height;
            if (screenW <= 0 || screenH <= 0)
            {
                Publish(KeyboardGeometrySnapshot.Unknown(KeyboardProviderId.Android));
                return;
            }

            var covered = KeyboardOcclusion.BottomCoveredPixels(viewY, screenH, frameBottom);
            if (!imeVisible && !TouchScreenKeyboard.visible)
            {
                lock (_legacyGate)
                {
                    _restingCovered = covered;
                }

                Publish(KeyboardGeometrySnapshot.Hidden(KeyboardProviderId.Android));
                return;
            }

            // The panel is still Screen.height tall from the view's top, even if the native view already shrunk.
            var occlusions = new KeyboardOcclusionList();
            for (var i = 0; i < imeCount; i++)
            {
                var offset = i * 4;
                if (KeyboardOcclusion.TryNormalizeIntersection(
                        _imeCopy[offset],
                        _imeCopy[offset + 1],
                        _imeCopy[offset + 2],
                        _imeCopy[offset + 3],
                        viewX,
                        viewY,
                        screenW,
                        screenH,
                        out var normalized))
                {
                    occlusions.Add(normalized);
                }
            }

            if (resting >= 0)
            {
                var keyboard = Math.Max(0, covered - resting);
                if (keyboard > 0)
                {
                    var height = keyboard / (float)screenH;
                    occlusions.Add(new NormalizedRect(0f, 1f - height, 1f, height));
                }
            }

            Publish(KeyboardGeometrySnapshot.Create(
                KeyboardVisibility.Visible, true, KeyboardProviderId.Android, occlusions));
        }

        private void MeasureLegacyGap()
        {
            if (_decor == null || _frame == null)
            {
                return;
            }

            _decor.Call("getWindowVisibleDisplayFrame", _frame);
            var frameBottom = _frame.Get<int>("bottom");
            if (!TryReadView(out _, out var viewY, out var viewW, out var viewH))
            {
                return;
            }

            var gap = Math.Max(0, viewY + viewH - frameBottom);
            lock (_legacyGate)
            {
                _currentGap = gap;
                _viewW = viewW;
                _viewH = viewH;
                _legacyValid = true;
                _legacyGeneration++;
            }
        }

        private void PublishLegacy()
        {
            int gap;
            int resting;
            int viewW;
            int viewH;
            int restingW;
            int generation;
            bool valid;
            lock (_legacyGate)
            {
                valid = _legacyValid;
                gap = _currentGap;
                resting = _restingGap;
                viewW = _viewW;
                viewH = _viewH;
                restingW = _restingViewW;
                generation = _legacyGeneration;
            }

            var screenH = Screen.height;
            var covered = screenH > 0 ? gap + screenH - viewH : gap;
            KeyboardAvoidanceDebug.Log("android.legacy",
                "valid=" + valid + " tskVisible=" + TouchScreenKeyboard.visible + " screen=" + Screen.width + "x" + screenH
                + " host=" + viewW + "x" + viewH + " gap=" + gap
                + " covered=" + covered + " resting=" + resting + " restingW=" + restingW + " generation=" + generation);

            if (!TouchScreenKeyboard.visible)
            {
                // Only a sample taken after the keyboard closed can become the baseline. The last
                // open-keyboard gap is larger and must not be stored as the closed position.
                if (valid && generation > _acceptRestingAfter)
                {
                    lock (_legacyGate)
                    {
                        _restingGap = covered;
                        _restingViewW = viewW;
                    }
                }
                else
                {
                    _acceptRestingAfter = generation;
                    RequestLegacySample();
                }

                _legacyVisible = false;
                Publish(KeyboardGeometrySnapshot.Hidden(KeyboardProviderId.Android));
                return;
            }

            _acceptRestingAfter = int.MaxValue;

            if (!_legacyVisible)
            {
                _legacyVisible = true;
                _openBaselineGeneration = generation;
            }

            // A height change is the keyboard resizing the view. A width change is rotation, which
            // makes the closed-keyboard baseline incomparable.
            if (!valid || resting < 0 || viewH <= 0 || viewW != restingW
                || generation <= _openBaselineGeneration)
            {
                if (!valid || generation <= _openBaselineGeneration)
                {
                    RequestLegacySample();
                }

                Publish(KeyboardGeometrySnapshot.Unknown(KeyboardProviderId.Android));
                return;
            }

            var keyboard = Math.Max(0, covered - resting);
            var denominator = screenH > 0 ? screenH : viewH;
            var occlusions = new KeyboardOcclusionList();
            if (keyboard > 0 && denominator > 0)
            {
                var height = keyboard / (float)denominator;
                occlusions.Add(new NormalizedRect(0f, 1f - height, 1f, height));
            }

            Publish(KeyboardGeometrySnapshot.Create(KeyboardVisibility.Visible, true, KeyboardProviderId.Android, occlusions));
        }

        private bool TryReadView(out int x, out int y, out int width, out int height)
        {
            x = 0;
            y = 0;
            width = 0;
            height = 0;
            if (_host == null)
            {
                return false;
            }

            _host.Call("getLocationOnScreen", _location);
            x = _location[0];
            y = _location[1];
            width = _host.Call<int>("getWidth");
            height = _host.Call<int>("getHeight");
            return width > 0 && height > 0;
        }

        private void RequestLegacySample()
        {
            var activity = _activity;
            if (activity == null || _disposed)
            {
                return;
            }

            lock (_legacyGate)
            {
                if (_legacySampleQueued)
                {
                    return;
                }

                _legacySampleQueued = true;
            }

            try
            {
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    lock (_legacyGate)
                    {
                        _legacySampleQueued = false;
                    }

                    if (_disposed)
                    {
                        return;
                    }

                    try
                    {
                        MeasureLegacyGap();
                    }
                    catch (Exception)
                    {
                        // The next poll requests another sample.
                    }
                }));
            }
            catch (Exception)
            {
                lock (_legacyGate)
                {
                    _legacySampleQueued = false;
                }
            }
        }

        private void RequestInsets()
        {
            var activity = _activity;
            var observer = _observer;
            if (activity == null || observer == null || _disposed)
            {
                return;
            }

            try
            {
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    if (_disposed)
                    {
                        return;
                    }

                    try
                    {
                        observer.Call("requestApplyInsets");
                    }
                    catch (Exception)
                    {
                        // The next inset dispatch will refresh the measurement.
                    }
                }));
            }
            catch (Exception)
            {
                // The activity is no longer able to accept work.
            }
        }

        private void Fail(string message, bool permanent)
        {
            if (permanent)
            {
                _permanentFailure = true;
            }

            Publish(KeyboardGeometrySnapshot.Unknown(KeyboardProviderId.Android), message);
        }

        // Keeps the Java callback reachable for IL2CPP. The flag is never set.
        private void PinListener()
        {
            if (_pinListener && _listener != null)
            {
                _listener.onApplyWindowInsets(null, null);
            }
        }

        private static AndroidJavaObject FindHost(AndroidJavaObject activity)
        {
            try
            {
                using var player = activity.Call<AndroidJavaObject>("getUnityPlayerConnection");
                if (player != null)
                {
                    var frame = player.Call<AndroidJavaObject>("getFrameLayout");
                    if (frame != null)
                    {
                        return frame;
                    }
                }
            }
            catch (Exception)
            {
                // GameActivity's player does not expose getFrameLayout. The content root is the fallback.
            }

            using var window = activity.Call<AndroidJavaObject>("getWindow");
            using var decor = window?.Call<AndroidJavaObject>("getDecorView");
            if (decor == null)
            {
                return null;
            }

            using var id = new AndroidJavaClass("android.R$id");
            return decor.Call<AndroidJavaObject>("findViewById", id.GetStatic<int>("content"));
        }

        private sealed class InsetsListener : AndroidJavaProxy
        {
            public InsetsListener(AndroidKeyboardGeometryProvider owner)
                : base("android.view.View$OnApplyWindowInsetsListener")
            {
                _owner = owner;
            }

            [Preserve]
            public AndroidJavaObject onApplyWindowInsets(AndroidJavaObject view, AndroidJavaObject insets)
            {
                _owner.OnInsets(insets);
                return insets;
            }

            private readonly AndroidKeyboardGeometryProvider _owner;
        }

        private readonly object _legacyGate = new object();
        private readonly int[] _location = new int[2];
        private readonly int[] _imeBounds = new int[16];
        private readonly int[] _imeCopy = new int[16];
        private AndroidJavaObject _activity;
        private AndroidJavaObject _decor;
        private AndroidJavaObject _host;
        private AndroidJavaObject _observer;
        private AndroidJavaObject _frame;
        private InsetsListener _listener;
        private int _sdk;
        private int _imeType;
        private int _imeCount;
        private int _rawViewX;
        private int _rawViewY;
        private int _rawViewW;
        private int _rawViewH;
        private int _rawFrameBottom;
        private int _restingCovered = -1;
        private bool _imeVisible;
        private bool _rawValid;
        private int _currentGap = -1;
        private int _restingGap = -1;
        private int _viewW;
        private int _viewH;
        private int _restingViewW;
        private int _legacyGeneration;
        private int _openBaselineGeneration = -1;
        private int _acceptRestingAfter = -1;
        private bool _legacyValid;
        private bool _legacyVisible;
        private bool _legacySampleQueued;
        private bool _started;
        private bool _startQueued;
        private bool _disposed;
        private bool _permanentFailure;
        private bool _pinListener;
    }
}
#endif
