using UnityEngine.UIElements;

namespace StreamVideo.ExampleProject.UIToolkit.Views
{
    internal static class CallControls
    {
        /// <summary>
        /// Reflect the device enabled state on a round toggle button
        /// </summary>
        public static void UpdateDeviceButton(Button button, VisualElement icon, bool isEnabled, string onIconClass,
            string offIconClass)
        {
            button.EnableInClassList("is-off", !isEnabled);
            icon.EnableInClassList(onIconClass, isEnabled);
            icon.EnableInClassList(offIconClass, !isEnabled);
        }
    }
}
