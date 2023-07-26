using UnityEngine;

namespace StreamVideo.Libs.AppInfo
{
    public class UnityApplicationInfo : IApplicationInfo
    {
        public string Engine => "Unity";
        
        public string EngineVersion => Application.unityVersion;
        
        public string Platform => Application.platform.ToString();
        
        public string OperatingSystem => SystemInfo.operatingSystem;
        public string OperatingSystemFamily => SystemInfo.operatingSystemFamily.ToString();
        public string CpuArchitecture => SystemInfo.processorType;

        public int MemorySize => SystemInfo.systemMemorySize;
        
        public int GraphicsMemorySize => SystemInfo.graphicsMemorySize;

        public string ScreenSize => Screen.width + "x" + Screen.height;
        
        //StreamTodo: solve this, the deviceName is just a local name so perhaps not something we want 
        public string DeviceName => SystemInfo.deviceName; 
        public string DeviceModel => SystemInfo.deviceModel;
    }
}