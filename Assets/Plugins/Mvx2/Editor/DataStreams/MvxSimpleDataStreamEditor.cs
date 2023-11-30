using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxSimpleDataStream), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxSimpleDataStreamEditor : MvxDataStreamEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawPlaybackModeProperty();
            DrawFollowFPSProperty();

            EditorGUI.BeginDisabledGroup(((MvxSimpleDataStream)target).playAtMaxFPS);
            DrawPlaybackSpeedProperty();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPlaybackModeProperty()
        {
            this.DrawPropertyEnum<MvxSimpleDataStream, Mvx2API.RunnerPlaybackMode>(new GUIContent("Playback mode"), x => x.playbackMode, (x, value) => x.playbackMode = value);
        }

        private void DrawFollowFPSProperty()
        {
            this.DrawPropertyToggle<MvxSimpleDataStream>(new GUIContent("Play at max FPS", "Play at the maximum possible FPS the hardware allows"), x => x.playAtMaxFPS, (x, value) => x.playAtMaxFPS = value);
        }

        private void DrawPlaybackSpeedProperty()
        {
            this.DrawPropertySlider<MvxSimpleDataStream>(new GUIContent("Playback speed"), x => x.playbackSpeed, (x, value) => x.playbackSpeed = value, 0, 3);
        }
    }
}