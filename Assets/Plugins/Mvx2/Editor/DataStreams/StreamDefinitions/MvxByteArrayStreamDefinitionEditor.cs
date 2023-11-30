using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxByteArrayStreamDefinition), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxByteArrayStreamDefinitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "MvxByteArrayStreamDefinition properties changed");

            DrawDefaultInspector();

            DrawTextAssetProperty();
        }

        private void DrawTextAssetProperty()
        {
            TextAsset textAsset = ((MvxByteArrayStreamDefinition)target).mvxTextAsset;
            bool mixedValue = false;

            foreach (object targetObject in targets)
                mixedValue = mixedValue || ((MvxByteArrayStreamDefinition)targetObject).mvxTextAsset != textAsset;

            bool originalShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = mixedValue;
            TextAsset newTextAsset = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Mvx Text Asset"), textAsset, typeof(TextAsset), true);
            EditorGUI.showMixedValue = originalShowMixedValue;

            if (textAsset != newTextAsset)
            {
                foreach (object targetObject in targets)
                {
                    ((MvxByteArrayStreamDefinition)targetObject).mvxTextAsset = newTextAsset;
                    EditorUtility.SetDirty(((MvxByteArrayStreamDefinition)targetObject));
                }
            }
        }
    }
}