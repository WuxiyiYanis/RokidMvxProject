using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxRemoteFileDataStreamDefinition), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxRemoteFileDataStreamDefinitionEditor : Editor
    {
        private readonly GUIContent m_fileURLGuiContent = new GUIContent("File URL", "URL of the remote file to read");
        private readonly GUIContent m_employLookupTableGuiContent = new GUIContent("Employ Look-up Table", "Specifies whether a Look-up Table shall be employed if present in the file");
        private readonly GUIContent m_sslVerificationModeGuiContent = new GUIContent("SSL Verification Mode", "Requirements for the server certificate verification when a TLS/SSL connection is negotiated");
        private readonly GUIContent m_chacheFileGuiContent = new GUIContent("Cache File Path", "Path to the file that will be written to, can be either absolute or relative (writes to temporaryCachePath)");
        private readonly GUIContent m_slowDownloadBlocksGuiContent = new GUIContent("Slow Download Blocks Playback", "If true, a slow downloading speed will block playback until enough of the file is downloaded to ensure smooth playback");
        private readonly GUIContent m_modeCurlGuiContent = new GUIContent("Curl mode", "Which curl implementation to use");

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "MvxRemoteFileAsyncDataStreamDefinition properties changed");

            DrawDefaultInspector();
            
            DrawFileURLProperty();
            DrawEmployLookupTableProperty();
            DrawSSLVerificationModeProperty();

            GUILayout.Space(10);
            DrawCurlModeProperty();
            if (((MvxRemoteFileDataStreamDefinition)target).curlMode == MvxRemoteFileDataStreamDefinition.CurlMode.Caching)
            {
                DrawCacheFileProperty();
                DrawSlowDownloadBlocksProperty();
            }
            GUILayout.Space(10);
        }

        private void DrawFileURLProperty()
        {
            this.DrawPropertyTextField<MvxRemoteFileDataStreamDefinition>(m_fileURLGuiContent, x => x.fileURL, (x, value) => x.fileURL = value);
        }

        private void DrawCacheFileProperty()
        {
            this.DrawPropertyTextField<MvxRemoteFileDataStreamDefinition>(m_chacheFileGuiContent, x => x.cacheFile, (x, value) => x.cacheFile = value);
        }

        private void DrawEmployLookupTableProperty()
        {
            this.DrawPropertyToggle<MvxRemoteFileDataStreamDefinition>(m_employLookupTableGuiContent, x => x.employLookupTable, (x, value) => x.employLookupTable = value);
        }

        private void DrawSSLVerificationModeProperty()
        {
			this.DrawPropertyEnum<MvxRemoteFileDataStreamDefinition, MvxRemoteFileDataStreamDefinition.SSLVerificationMode>
				(m_sslVerificationModeGuiContent, x => x.sslVerificationMode, (x, value) => x.sslVerificationMode = value);
		}

        private void DrawSlowDownloadBlocksProperty()
        {
            this.DrawPropertyToggle<MvxRemoteFileDataStreamDefinition>(m_slowDownloadBlocksGuiContent, x => x.slowDownloadBlocks, (x, value) => x.slowDownloadBlocks = value);
        }

        private void DrawCurlModeProperty()
        {
            this.DrawPropertyEnum<MvxRemoteFileDataStreamDefinition, MvxRemoteFileDataStreamDefinition.CurlMode>
                (m_modeCurlGuiContent, x => x.curlMode, (x, value) => x.curlMode = value);
        }
    }
}