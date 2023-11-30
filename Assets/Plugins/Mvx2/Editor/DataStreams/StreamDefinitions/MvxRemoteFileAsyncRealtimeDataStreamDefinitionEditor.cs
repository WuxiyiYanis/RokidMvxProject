using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxRemoteFileAsyncRealtimeDataStreamDefinition), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxRemoteFileAsyncRealtimeDataStreamDefinitionEditor : MvxRemoteFileDataStreamDefinitionEditor
    {
        private readonly GUIContent m_bufferSizeGuiContent = new GUIContent("Buffer Size", "A size of the frames-buffer populated from a downloading thread and depopulated from the pipeline thread");
        private readonly GUIContent m_modeGuiContent = new GUIContent("Mode", "A frames-requesting mode manifested when downloading of frames is slower than playback in a pipeline thread");
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawBufferSizeProperty();
            DrawModeProperty();
        }

        private void DrawBufferSizeProperty()
        {
            this.DrawPropertyIntField<MvxRemoteFileAsyncRealtimeDataStreamDefinition>(m_bufferSizeGuiContent, x => (int)x.bufferSize, (x, value) => x.bufferSize = (uint)value);
        }

        private void DrawModeProperty()
        {
            this.DrawPropertyEnum<MvxRemoteFileAsyncRealtimeDataStreamDefinition, MvxRemoteFileAsyncRealtimeDataStreamDefinition.Mode>
                (m_modeGuiContent, x => x.mode, (x, value) => x.mode = value);
        }
    }
}