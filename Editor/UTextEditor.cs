using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Typography_Editor
{
    [CustomEditor(typeof(UText))]
    public class UTextEditor : GraphicEditor
    {
        SerializedProperty _font;
        SerializedProperty _text;
        SerializedProperty _fontSize;
        SerializedProperty _alignment;

        protected override void OnEnable()
        {
            base.OnEnable();
            _font = serializedObject.FindProperty("_font");
            _text = serializedObject.FindProperty("_text");
            _fontSize = serializedObject.FindProperty("_fontSize");
            _alignment = serializedObject.FindProperty("_alignment");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_font);
            EditorGUILayout.PropertyField(_text);
            EditorGUILayout.PropertyField(_fontSize);
            EditorGUILayout.PropertyField(_alignment);

            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}