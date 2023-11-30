using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MVXUnity
{
	public static class MvxEditorExtensions
	{
        public static void DrawPropertyObject<Target, T>(this Editor editor, GUIContent title, Func<Target, T> getter, Action<Target, T> setter) where Target : UnityEngine.Object where T : UnityEngine.Object
        {
            (bool mixedValue, T value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            T newValue = (T)
                EditorGUILayout.ObjectField(title, value, typeof(T), true); 

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }

        public static void DrawPropertyEnum<Target, T>(this Editor editor, GUIContent title, Func<Target, T> getter, Action<Target, T> setter) where Target : UnityEngine.Object where T : Enum
        {
            (bool mixedValue, T value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            T newValue = (T)
                EditorGUILayout.EnumPopup(title, value);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }

        public static void DrawPropertyToggle<Target>(this Editor editor, GUIContent title, Func<Target, bool> getter, Action<Target, bool> setter) where Target : UnityEngine.Object
        {
            (bool mixedValue, bool value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            bool newValue =
                EditorGUILayout.Toggle(title, value);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);     
        }

        public static void DrawPropertyIntSlider<Target>(this Editor editor, GUIContent title, Func<Target, int> getter, Action<Target, int> setter, int min, int max) where Target : UnityEngine.Object
        {
            (bool mixedValue, int value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            int newValue =
                EditorGUILayout.IntSlider(title, value, min, max);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }

        public static void DrawPropertySlider<Target>(this Editor editor, GUIContent title, Func<Target, float> getter, Action<Target, float> setter, float min, float max) where Target : UnityEngine.Object
        {
            (bool mixedValue, float value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            float newValue =
                EditorGUILayout.Slider(title, value, min, max);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }

        public static void DrawPropertyTextField<Target>(this Editor editor, GUIContent title, Func<Target, string> getter, Action<Target, string> setter) where Target : UnityEngine.Object
        {
            (bool mixedValue, string value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            string newValue = 
                EditorGUILayout.TextField(title, value);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }
       
        public static void DrawPropertyIntField<Target>(this Editor editor, GUIContent title, Func<Target, int> getter, Action<Target, int> setter) where Target : UnityEngine.Object
        {
            (bool mixedValue, int value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            int newValue = 
                EditorGUILayout.IntField(title, value);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }     
        
        public static void DrawPropertyLongField<Target>(this Editor editor, GUIContent title, Func<Target, long> getter, Action<Target, long> setter) where Target : UnityEngine.Object
        {
            (bool mixedValue, long value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            long newValue = 
                EditorGUILayout.LongField(title, value);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }

        public static void DrawPropertyFloatField<Target>(this Editor editor, GUIContent title, Func<Target, float> getter, Action<Target, float> setter) where Target : UnityEngine.Object
        {
            (bool mixedValue, float value, bool originalShowMixedValue) = editor.DrawPropertyFirstPart(getter);

            float newValue =
                EditorGUILayout.FloatField(title, value);

            editor.DrawPropertySecondPart(setter, value, newValue, originalShowMixedValue);
        }




        private static (bool mixedValue, T value, bool originalShowMixedValue) DrawPropertyFirstPart<Target, T>(this Editor editor, Func<Target, T> getter) where Target : UnityEngine.Object
        {
            T value = getter.Invoke((Target)editor.target);
            bool mixedValue = false;

            foreach (object targetObject in editor.targets)
            {
                T otherValue = getter.Invoke((Target)targetObject);
                mixedValue = mixedValue || !Equals(value, otherValue);
            }

            bool originalShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = mixedValue;

            return (mixedValue, value, originalShowMixedValue);
        }

        private static void DrawPropertySecondPart<Target, T>(this Editor editor, Action<Target, T> setter, T value, T newValue, bool originalShowMixedValue)
        {
            EditorGUI.showMixedValue = originalShowMixedValue;

            if (!Equals(value, newValue))
            {
                EditorUtility.SetDirty(editor.target);
                foreach (object targetObject in editor.targets)
                    setter.Invoke((Target)targetObject, newValue);
            }
        }

        public static bool Equals<T>(T a, T b)
		{
            return (a == null && b == null) || (a != null && a.Equals(b));
		}
    }
}
