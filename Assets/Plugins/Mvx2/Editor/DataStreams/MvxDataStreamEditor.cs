using UnityEngine;
using UnityEditor;
using System;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxDataStream), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxDataStreamEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "MvxDataStream properties changed");

            DrawDefaultInspector();
            DrawDataStreamDefinitionProperty();
        }

        private void DrawDataStreamDefinitionProperty()
        {
            this.DrawPropertyObject<MvxDataStream, MvxDataStreamDefinition>(new GUIContent("Data stream definition"), (x) => x.dataStreamDefinition, (x, value) => x.dataStreamDefinition = value);
        }
    }
}