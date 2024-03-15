using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace StreamVideo.EditorTools.BuildPostprocess
{
    /// <summary>
    /// This script will automatically apply the necessary build settings required by the Stream Video & Audio SDK for IOS
    /// </summary>
    internal class IOSBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS
            if (report.summary.platform == BuildTarget.iOS)
            {
                try
                {
                    DisableBitcodeOnAllTargetsInXCode(report);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to apply Stream's Video & Audio SDK build settings for the IOS platform. " +
                                   "Please check the documentation and apply the necessary changes manually. Error: " + e.Message);
                }
            }
#endif
        }

#if UNITY_IOS
        private static void DisableBitcodeOnAllTargetsInXCode(BuildReport report)
        {
            var projectPath = report.summary.outputPath + "/Unity-iPhone.xcodeproj/project.pbxproj";

            var pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);

            // Disabling Bitcode on all targets

            // Main
            var target = pbxProject.GetUnityMainTargetGuid();
            pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

            // Unity Tests
            target = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
            pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

            // Unity Framework
            target = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

            pbxProject.WriteToFile(projectPath);
            
            Debug.Log("Stream Video & Audio SDK - Build Postprocess - Successfully set the `ENABLE_BITCODE` to `NO` in the generated Xcode Project");
        }
#endif
    }
}