#if UNITY_IOS

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.iOS.Xcode;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace MVXUnity
{
    /// <summary>
    /// Intention of this class is to do additional changes to generatex Xcode project.
    /// This script is called by Unity after finishing iOS build.
    /// </summary>
#if UNITY_2018_1_OR_NEWER
    public class PostprocessBuildIOS : IPostprocessBuildWithReport
#else
    public class PostprocessBuildIOS : IPostprocessBuild
#endif
    {
        private readonly string BUILD_PHASE_NAME = "Embed Mvx2 Frameworks";
        
        public int callbackOrder
        {
            get { return 999; } // No matter what is here
        }

#if UNITY_2018_1_OR_NEWER
    public void OnPostprocessBuild(BuildReport report)
        {
            OnPostprocessBuild(report.summary.platform, report.summary.outputPath);
        }
#endif

        /// <inheritdoc />
        /// <remarks>
        /// Postprocessing of generated Xcode project.
        /// Collects all user frameworks and adds them as embedded to Xcode project.
        /// </remarks>
        public void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
                return;

            Debug.Log("Mvx2: Started iOS post-build step");

            string projectPath = PBXProject.GetPBXProjectPath(path);
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(projectPath));

#if UNITY_2019_3_OR_NEWER
            string targetGuid = pbxProject.GetUnityMainTargetGuid();
#else
            string targetName = PBXProject.GetUnityTargetName();
            string targetGuid = pbxProject.TargetGuidByName(targetName);
#endif

            pbxProject.AddBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");
            
            HashSet<string> embeddedMvx2Frameworks = IdentifyEmbeddedMvx2Frameworks(pbxProject, BUILD_PHASE_NAME);
            Dictionary<string, string> mvx2Frameworks = FindMvx2Frameworks();
            
            HashSet<string> embeddedMvx2FrameworksToRemove = new HashSet<string>(embeddedMvx2Frameworks.Except(mvx2Frameworks.Keys));
            Dictionary<string, string> newMvx2Frameworks = new Dictionary<string, string>();
            foreach (var mvx2Framework in mvx2Frameworks)
            {
                if (!embeddedMvx2Frameworks.Contains(mvx2Framework.Key))
                    newMvx2Frameworks.Add(mvx2Framework.Key, mvx2Framework.Value);
            }
            
            // Purge no-more-present embedded Mvx2 frameworks from the PBX project
            PurgeEmbeddedMvx2Frameworks(pbxProject, projectPath, embeddedMvx2FrameworksToRemove);

            // Add new Mvx2 frameworks to the PBX project
            EmbedMvx2Frameworks(pbxProject, projectPath, targetGuid, BUILD_PHASE_NAME, newMvx2Frameworks);
            
            Debug.Log("Mvx2: Finished iOS post-build step");
        }
        
        /// <summary> Identifies Mvx2 frameworks already embedded in a PBX project. </summary>
        /// <param name="pbxProject"> a PBX project to look in </param>
        /// <param name="buildPhaseName"> a name of the build phase the frameworks are part of </param>
        /// <returns> a collection of identified embedded Mvx2 frameworks </returns>
        private HashSet<string> IdentifyEmbeddedMvx2Frameworks(PBXProject pbxProject, string buildPhaseName)
        {
            HashSet<string> foundFrameworks = new HashSet<string>();
            
            string pbxProjectString = pbxProject.WriteToString();
            string pattern = string.Format(@".*\s(\w+)\.framework in {0}", buildPhaseName);
            
            var match = Regex.Match(pbxProjectString, pattern, RegexOptions.None);
                
            while (match.Success)
            {
                var group = match.Groups[1];
                foundFrameworks.Add(string.Format("{0}.framework", group.Captures[0]));
                
                match = match.NextMatch();
            }
             
            return foundFrameworks;
        }
        
        /// <summary>
        /// Enumerates over all iOS frameworks found in Plugins/Mvx2.
        /// Returned path is relative to Assets folder
        /// </summary>
        /// <returns>Paths to all iOS frameworks found in Mvx2.</returns>
        private Dictionary<string, string> FindMvx2Frameworks()
        {
            var basePath = Application.dataPath + "/";
            var dirs = Directory.GetDirectories(
                basePath + "Plugins/Mvx2", "*.framework", SearchOption.AllDirectories);

            for (int i = 0; i < dirs.Length; ++i)
                dirs[i] = dirs[i].Replace(basePath, "").Replace('\\', '/');
                
            Dictionary<string, string> frameworks = new Dictionary<string, string>();
            
            foreach (string frameworkPath in dirs)
                frameworks[Path.GetFileName(frameworkPath)] = frameworkPath;

            return frameworks;
        }

        /// <summary> Cleans a PBX project of already embedded no-more-existing Mvx2 frameworks. </summary>
        /// <param name="pbxProject"> a PBX project to clean </param>
        /// <param name="projectPath"> a path of the PBX project </param>
        /// <param name="embeddedMvx2FrameworksToRemove"> a collection of embedded Mvx2 frameworks to remove </param>
        private void PurgeEmbeddedMvx2Frameworks(PBXProject pbxProject, string projectPath, HashSet<string> embeddedMvx2FrameworksToRemove)
        {
            if (embeddedMvx2FrameworksToRemove.Count == 0)
                return;
                
            Debug.LogFormat("Mvx2: Purging no-more-existing embedded Mvx2 frameworks from the PBX project: {0}", string.Join(", ", embeddedMvx2FrameworksToRemove));
                
            string pbxProjectString = pbxProject.WriteToString();
            StringBuilder pbxProjectNewStringBuilder = new StringBuilder();
            
            foreach (string line in pbxProjectString.Split('\n'))
            {
                bool lineMentionsAnMvx2Framework = false;
                
                foreach (string framework in embeddedMvx2FrameworksToRemove)
                {
                    if (line.Contains(framework))
                    {
                        lineMentionsAnMvx2Framework = true;
                        break;
                    }
                }
                
                if (!lineMentionsAnMvx2Framework)
                    pbxProjectNewStringBuilder.Append(line + "\n");
            }
            
            pbxProjectString = pbxProjectNewStringBuilder.ToString();
            pbxProject.ReadFromString(pbxProjectString);
            File.WriteAllText(projectPath, pbxProjectString);
        }
        
        /// <summary> Sets up embedding of new Mvx2 frameworks in a PBX project. </summary>
        /// <param name="pbxProject"> a PBX project </param>
        /// <param name="projectPath"> a path of the PBX project </param>
        /// <param name="targetGuid"> a GUID of the target to embed the frameworks within </param>
        /// <param name="buildPhaseName"> a name of the build phase the frameworks will be part of </param>
        /// <param name="newMvx2Frameworks"> a collection of the Mvx2 frameworks to be embedded </param>
        private void EmbedMvx2Frameworks(PBXProject pbxProject, string projectPath, string targetGuid, string buildPhaseName, Dictionary<string, string> newMvx2Frameworks)
        {
            if (newMvx2Frameworks.Count == 0)
                return;
            
            string buildPhaseGuid = pbxProject.AddCopyFilesBuildPhase(targetGuid, buildPhaseName, "", "10");
            foreach (string framework in newMvx2Frameworks.Values)
            {
                Debug.LogFormat("Mvx2: Adding new Mvx2 framework to the PBX project: {0}", framework);
                string frameworkPathInPBXProject = "Frameworks/" + framework;

                var frameworkFileGuid = pbxProject.FindFileGuidByProjectPath(frameworkPathInPBXProject);
                if (frameworkFileGuid == null)
                {
                    Debug.LogErrorFormat("Mvx2: Framework file `{0}` not found in the generated PBX project, skipping", frameworkPathInPBXProject);
                    continue;
                }

                pbxProject.AddFileToBuildSection(targetGuid, buildPhaseGuid, frameworkFileGuid);
            }
            
            // Enable Code Sign on Copy attribute for the new embedded frameworks
            string pbxProjectString = pbxProject.WriteToString();
            foreach (string framework in newMvx2Frameworks.Keys)
            {
                Debug.LogFormat("Mvx2: Enabling 'Code Sign on Copy' attribute for the new Mvx2 framework: {0}", framework);
                
                string pattern = string.Format(@"(.*{0} in {1} \*/ = .*) \}};", framework, buildPhaseName);
                string replacement = @"$1 settings = {ATTRIBUTES = (CodeSignOnCopy, ); }; };";
                
                pbxProjectString = Regex.Replace(pbxProjectString, pattern, replacement);
            }
            pbxProject.ReadFromString(pbxProjectString);
            File.WriteAllText(projectPath, pbxProjectString);
        }
    }
}

#endif
