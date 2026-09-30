using UnityEngine;
using UnityEngine.UIElements;
// Device.* reflects the simulated device when running in the Device Simulator
using Application = UnityEngine.Device.Application;
using Screen = UnityEngine.Device.Screen;

namespace StreamVideo.ExampleProject.UIToolkit
{
    /// <summary>
    /// Keeps a single layout usable on every screen:
    /// - scales the panel so that UI sizes match density-independent pixels on mobile,
    /// - toggles the `portrait` / `landscape` USS classes on the app root,
    /// - insets the content by <see cref="Screen.safeArea"/> (notches, rounded corners, home indicator).
    /// </summary>
    internal sealed class ScreenAdapter
    {
        public const string PortraitClass = "portrait";
        public const string LandscapeClass = "landscape";

        public float Scale => _panelSettings.scale;
        public bool IsLandscape { get; private set; }

        public ScreenAdapter(PanelSettings panelSettings, VisualElement appRoot, VisualElement safeArea)
        {
            _panelSettings = panelSettings;
            _appRoot = appRoot;
            _safeArea = safeArea;

            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            Update();
        }

        public void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (screenSize == _lastScreenSize && safeArea == _lastSafeArea)
            {
                return;
            }

            _lastScreenSize = screenSize;
            _lastSafeArea = safeArea;

            var scale = CalculateScale(screenSize);
            _panelSettings.scale = scale;

            _safeArea.style.marginLeft = safeArea.xMin / scale;
            _safeArea.style.marginRight = (screenSize.x - safeArea.xMax) / scale;
            _safeArea.style.marginTop = (screenSize.y - safeArea.yMax) / scale;
            _safeArea.style.marginBottom = safeArea.yMin / scale;

            IsLandscape = screenSize.x > screenSize.y;
            _appRoot.EnableInClassList(LandscapeClass, IsLandscape);
            _appRoot.EnableInClassList(PortraitClass, !IsLandscape);
        }

        // Mobile layouts are designed for a ~360-430 logical px wide portrait screen
        private const float MobileReferenceDpi = 160;
        private const float MinLogicalShortSide = 360;
        private const float DesktopReferenceDpi = 96;

        private readonly PanelSettings _panelSettings;
        private readonly VisualElement _appRoot;
        private readonly VisualElement _safeArea;

        private Vector2Int _lastScreenSize;
        private Rect _lastSafeArea;

        private static float CalculateScale(Vector2Int screenSize)
        {
            var shortSide = Mathf.Min(screenSize.x, screenSize.y);
            var dpi = Screen.dpi;

            if (Application.isMobilePlatform)
            {
                var scale = dpi > 0 ? dpi / MobileReferenceDpi : shortSide / 390f;
                return Mathf.Max(1f, Mathf.Min(scale, shortSide / MinLogicalShortSide));
            }

            return dpi > 0 ? Mathf.Max(1f, dpi / DesktopReferenceDpi) : 1f;
        }
    }
}
