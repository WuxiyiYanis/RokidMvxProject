using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxAudioPlayerStream), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxAudioPlayerStreamEditor : MvxDataStreamEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawPlaybackModeProperty();
            DrawPlaybackSpeedProperty();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);

            DrawMuteProperty();
            DrawVolumeProperty();
        }

        private void DrawPlaybackModeProperty()
        {
			this.DrawPropertyEnum<MvxAudioPlayerStream, MvxAudioPlayerStream.AudioPlaybackMode>
                (new GUIContent("Playback mode"), x => x.playbackMode, (x, value) => x.playbackMode = value);
        }

        private void DrawMuteProperty()
        {
            this.DrawPropertyToggle<MvxAudioPlayerStream>(new GUIContent("Mute"), x => x.mute, (x, value) => x.mute = value);
        }

        private void DrawVolumeProperty()
        {
            this.DrawPropertySlider<MvxAudioPlayerStream>(new GUIContent("Volume"), x => x.volume, (x, value) => x.volume = value, 0, 1);
        }

        private void DrawPlaybackSpeedProperty()
        {
            this.DrawPropertySlider<MvxAudioPlayerStream>(new GUIContent("Playback speed"), x => x.playbackSpeed, (x, value) => x.playbackSpeed = value, 0, 3);
        }
    }
}