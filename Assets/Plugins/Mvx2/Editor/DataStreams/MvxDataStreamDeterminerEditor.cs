using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxDataStreamDeterminer), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxDataStreamDeterminerEditor : MvxDataStreamEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawPlaybackModeProperty();
            DrawFollowFPSProperty();
            DrawMinimalBufferedAudioDurationProperty();
            DrawAudioStreamEnabledProperty();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);

            DrawMuteProperty();
            DrawVolumeProperty();
        }

        private void DrawPlaybackModeProperty()
        {
            this.DrawPropertyEnum<MvxDataStreamDeterminer, Mvx2API.RunnerPlaybackMode>(new GUIContent("Playback mode"), x => x.playbackMode, (x, value) => x.playbackMode = value);
        }

        private void DrawFollowFPSProperty()
        {
            this.DrawPropertyToggle<MvxDataStreamDeterminer>(new GUIContent("Play at max FPS", "Play at the maximum possible FPS the hardware allows"), x => x.playAtMaxFPS, (x, value) => x.playAtMaxFPS = value);
        }

        private void DrawMinimalBufferedAudioDurationProperty()
        {
            this.DrawPropertyFloatField<MvxDataStreamDeterminer>(new GUIContent("Minimal buffered audio duration"), x => x.minimalBufferedAudioDuration, (x, value) => x.minimalBufferedAudioDuration = value);
        }

        private void DrawAudioStreamEnabledProperty()
        {
            this.DrawPropertyToggle<MvxDataStreamDeterminer>(new GUIContent("Audio stream enabled"), x => x.audioStreamEnabled, (x, value) => x.audioStreamEnabled = value);
        }

        private void DrawMuteProperty()
        {
            this.DrawPropertyToggle<MvxDataStreamDeterminer>(new GUIContent("Mute"), x => x.mute, (x, value) => x.mute = value);
        }

        private void DrawVolumeProperty()
        {
            this.DrawPropertySlider<MvxDataStreamDeterminer>(new GUIContent("Volume"), x => x.volume, (x, value) => x.volume = value, 0, 1);
        }
    }
}