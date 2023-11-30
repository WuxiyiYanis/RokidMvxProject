using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxDataRandomAccessStream), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxDataRandomAccessStreamEditor : MvxDataStreamEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawFrameIDProperty();
        }

        private void DrawFrameIDProperty()
        {
            this.DrawPropertyIntField<MvxDataRandomAccessStream>(new GUIContent("Frame ID"), x => (int)x.frameId, (x, value) => x.frameId = (uint)value);
        }
    }
}