using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxRemoteFileSyncDataStreamDefinition), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxRemoteFileSyncDataStreamDefinitionEditor : MvxRemoteFileDataStreamDefinitionEditor
    {
        private readonly GUIContent m_frameCacheTypeGuiContent = new GUIContent("Frame Cache Type", "A type of frame cache to be used for caching already downloaded frames");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawFrameCacheTypeProperty();
        }

        private void DrawFrameCacheTypeProperty()
        {
            this.DrawPropertyEnum<MvxRemoteFileSyncDataStreamDefinition, MvxRemoteFileSyncDataStreamDefinition.FrameCacheType>
                (m_frameCacheTypeGuiContent, x => x.frameCacheType, (x, value) => x.frameCacheType = value);
        }
    }
}