using System;
using UnityEngine;
// Device.* reflects the simulated device when running in the Device Simulator
using Application = UnityEngine.Device.Application;
using Screen = UnityEngine.Device.Screen;

namespace StreamVideo.ExampleProject.UIToolkit
{
    /// <summary>
    /// How far the software keyboard covers the screen, measured in screen pixels from the bottom edge.
    /// Zero on desktop and while the keyboard is closed. A keyboard that already lies below
    /// <see cref="Screen.height"/> contributes nothing, so a view the platform already resized is not inset again.
    /// </summary>
    internal static class MobileKeyboard
    {
        public static float CoverageFromBottom()
        {
            if (!Application.isMobilePlatform)
            {
                return 0f;
            }

            if (_frame == Time.frameCount)
            {
                return _coverage;
            }

            _frame = Time.frameCount;
            _coverage = Calculate();
            return _coverage;
        }

        private static int _frame = -1;
        private static float _coverage;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool _androidFailed;
        private static AndroidJavaObject _decorView;
        private static AndroidJavaObject _visibleFrame;
#endif

        private static float Calculate()
        {
            // area can stay non-zero after the keyboard closes, so visibility gates the inset.
            if (!TouchScreenKeyboard.visible)
            {
                return 0f;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // TouchScreenKeyboard.area is often empty on Android. The visible display frame is the
            // region the keyboard has not covered. adjustResize already shrinks the view, so the
            // difference stays ~0 and the inset is not applied twice.
            var occlusion = AndroidBottomOcclusion();
            if (occlusion >= 0f)
            {
                return occlusion;
            }
#endif
            return OverlapFromBottom(TouchScreenKeyboard.area, Screen.height);
        }

        internal static float OverlapFromBottom(Rect keyboard, float screenHeight)
        {
            if (keyboard.height <= 0f || screenHeight <= 0f)
            {
                return 0f;
            }

            var overlap = Mathf.Min(keyboard.yMax, screenHeight) - Mathf.Max(keyboard.yMin, 0f);
            return Mathf.Max(0f, overlap);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static float AndroidBottomOcclusion()
        {
            if (_androidFailed)
            {
                return -1f;
            }

            try
            {
                if (!EnsureAndroidView())
                {
                    return -1f;
                }

                _decorView.Call("getWindowVisibleDisplayFrame", _visibleFrame);
                var viewHeight = _decorView.Call<int>("getHeight");
                if (viewHeight <= 0)
                {
                    return -1f;
                }

                var covered = Mathf.Max(0, viewHeight - _visibleFrame.Get<int>("bottom"));
                return covered * (Screen.height / viewHeight);
            }
            catch (Exception e)
            {
                _androidFailed = true;
                _decorView?.Dispose();
                _visibleFrame?.Dispose();
                _decorView = null;
                _visibleFrame = null;
                Debug.LogWarning($"Could not measure the Android keyboard: {e.Message}");
                return -1f;
            }
        }

        private static bool EnsureAndroidView()
        {
            if (_decorView != null)
            {
                return true;
            }

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null)
            {
                return false;
            }

            using var window = activity.Call<AndroidJavaObject>("getWindow");
            if (window == null)
            {
                return false;
            }

            _decorView = window.Call<AndroidJavaObject>("getDecorView");
            _visibleFrame = new AndroidJavaObject("android.graphics.Rect");
            return _decorView != null && _visibleFrame != null;
        }
#endif
    }
}
