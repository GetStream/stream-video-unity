#if UNITY_EDITOR
using StreamVideo.Libs.PackageXmlInstaller;

namespace StreamVideo.Core
{
    /// <summary> 
    /// Link Xml installer for StreamVideo.Core assembly
    /// </summary>
    public class CoreAssemblyLinkXmlInstaller : PackageXmlInstallerBase
    {
        public override string LinkXmlGuid => "f463da9835a43604c81158483e96cac3";
    }
}
#endif