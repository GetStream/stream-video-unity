#if UNITY_EDITOR
using StreamVideo.Libs.PackageXmlInstaller;

namespace StreamVideo.Libs
{
    /// <summary> 
    /// Link Xml installer for StreamVideo.Libs assembly
    /// </summary>
    public class LibsAssemblyLinkXmlInstaller : PackageXmlInstallerBase
    {
        public override string LinkXmlGuid => "57e429f792d04b47828748874c202c2f";
    }
}
#endif