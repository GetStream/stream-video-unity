#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace StreamVideo.Libs.PackageXmlInstaller
{
    /// <summary>
    /// This is needed because UPM packages do not support link.xml.
    /// Unity suggested workaround is to use IUnityLinkerProcessor to include our custom link.xml during build process
    /// More info in the thread:
    /// https://discussions.unity.com/t/the-current-state-of-link-xml-in-packages/814559/5
    /// </summary>
    public abstract class PackageXmlInstallerBase : IUnityLinkerProcessor
    {
        public abstract string LinkXmlGuid { get; }
        
        int IOrderedCallback.callbackOrder => 0;

        string IUnityLinkerProcessor.GenerateAdditionalLinkXmlFile(BuildReport report,
            UnityLinkerBuildPipelineData data)
        {
            //StreamTodo: ensure with a test that GUID exists

            var assetPath = AssetDatabase.GUIDToAssetPath(LinkXmlGuid);
            // assets paths are relative to the unity project root, but they don't correspond to actual folders for
            // Packages that are embedded. I.e. it won't work if a package is installed as a git submodule
            // So resolve it to an absolute path:
            return Path.GetFullPath(assetPath);
        }
    }
}
#endif