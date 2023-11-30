using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxRemoteFileAsyncDataStreamDefinition), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxRemoteFileAsyncDataStreamDefinitionEditor : MvxRemoteFileDataStreamDefinitionEditor
    {
        private readonly GUIContent m_bufferSizeGuiContent = new GUIContent("Buffer Size", "A size of the frames-buffer populated from a downloading thread and depopulated from the pipeline thread");
        private readonly GUIContent m_modeGuiContent = new GUIContent("Mode", "A frames-requesting mode manifested when downloading of frames is slower than playback in a pipeline thread");
        private readonly GUIContent m_frameCacheTypeGuiContent = new GUIContent("Frame Cache Type", "A type of frame cache to be used for caching already downloaded frames");
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawBufferSizeProperty();
            DrawModeProperty();
            DrawFrameCacheTypeProperty();
        }

        private void DrawBufferSizeProperty()
        {
            this.DrawPropertyIntField<MvxRemoteFileAsyncDataStreamDefinition>(m_bufferSizeGuiContent, x => (int)x.bufferSize, (x, value) => x.bufferSize = (uint)value);
        }

        private void DrawModeProperty()
        {
            this.DrawPropertyEnum<MvxRemoteFileAsyncDataStreamDefinition, MvxRemoteFileAsyncDataStreamDefinition.Mode>
                (m_modeGuiContent, x => x.mode, (x, value) => x.mode = value);
        }

        private void DrawFrameCacheTypeProperty()
        {
            this.DrawPropertyEnum<MvxRemoteFileAsyncDataStreamDefinition, MvxRemoteFileAsyncDataStreamDefinition.FrameCacheType>
                (m_frameCacheTypeGuiContent, x => x.frameCacheType, (x, value) => x.frameCacheType = value);
        }
    }
}