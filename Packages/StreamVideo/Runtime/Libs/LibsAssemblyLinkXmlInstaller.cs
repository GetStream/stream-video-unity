#if UNITY_EDITOR
using StreamVideo.Libs.PackageXmlInstaller;

namespace StreamVideo.Libs
{
    /// <summary> 
    /// Link Xml installer for StreamVideo.Libs assembly
    /// </summary>
    public class LibsAssemblyLinkXmlInstaller : PackageXmlInstallerBase
    {
        public override string LinkXmlGuid => "67e548a9d13c984408549b00e07ae9bc";
    }
}
#endif