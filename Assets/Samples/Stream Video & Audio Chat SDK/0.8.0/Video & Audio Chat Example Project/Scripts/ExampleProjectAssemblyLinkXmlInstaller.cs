#if UNITY_EDITOR
using StreamVideo.Libs.PackageXmlInstaller;

namespace StreamVideo.ExampleProject
{
    /// <summary> 
    /// Link Xml installer for StreamVideo.ExampleProject assembly
    /// </summary>
    public class ExampleProjectAssemblyLinkXmlInstaller : PackageXmlInstallerBase
    {
        public override string LinkXmlGuid => "57e429f792d04b47828748874c202c2f";
    }
}
#endif