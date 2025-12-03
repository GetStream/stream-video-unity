using UnityEngine;

namespace StreamVideo.Libs.AppInfo
{
    public class UnityApplicationInfo : IApplicationInfo
    {
        public string Engine => "Unity";

        public string EngineVersion => Application.unityVersion;

        public string Platform => Application.platform.ToString();

        public string OperatingSystem => SystemInfo.operatingSystem;

        public string OperatingSystemFamily => GetOsFamily();

        public string CpuArchitecture => SystemInfo.processorType;

        public int MemorySize => SystemInfo.systemMemorySize;

        public int GraphicsMemorySize => SystemInfo.graphicsMemorySize;

        public string ScreenSize => Screen.width + "x" + Screen.height;

        //StreamTodo: solve this, the deviceName is just a local name so perhaps not something we want 
        public string DeviceName => SystemInfo.deviceName;

        public string DeviceModel => SystemInfo.deviceModel;

        private static string GetOsFamily()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "Android";

                case RuntimePlatform.IPhonePlayer:
                    return "iOS";

                case RuntimePlatform.tvOS:
                    return "tvOS";

                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "macOS";

                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "Windows";

                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
                case RuntimePlatform.LinuxEditor:
                    return "Linux";

                default:
                    var family = SystemInfo.operatingSystemFamily;
                    return family != UnityEngine.OperatingSystemFamily.Other ? family.ToString() : "Unknown";
            }
        }
    }
}
