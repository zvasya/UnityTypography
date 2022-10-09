using UnityEditor;
using UnityEngine;

namespace Typography_Editor
{
    [CustomPropertyDrawer(typeof(TextAnchor), true)]
    public class AlignmentPropertyDrawer : PropertyDrawer
    {
        private enum VAlignment
        {
            Top,
            Center,
            Bottom
        }

        private enum HAlignment
        {
            Left,
            Center,
            Right
        }

        private GUIContent[] horizontalAlignment =
        {
            EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_left", "Left Align"),
            EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_center", "Center Align"),
            EditorGUIUtility.IconContent(@"GUISystem/align_horizontally_right", "Right Align"),
        };
        
        private GUIContent[] verticalAlignment =
        {
            EditorGUIUtility.IconContent(@"GUISystem/align_vertically_top", "Top Align"),
            EditorGUIUtility.IconContent(@"GUISystem/align_vertically_center", "Middle Align"),
            EditorGUIUtility.IconContent(@"GUISystem/align_vertically_bottom", "Bottom Align"),
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            var value = (TextAnchor)prop.enumValueIndex;

            var (hAlign, vAlign) = SeparateAxis(value);
            EditorGUI.BeginProperty(position, label, prop);
            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth;
            labelRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PrefixLabel(position, label);

            EditorGUI.BeginChangeCheck();

            var rect = position;
            rect.width = (rect.width - labelRect.width - EditorGUIUtility.standardVerticalSpacing) / 2;
            rect.x += labelRect.width + EditorGUIUtility.standardVerticalSpacing;
            hAlign = (HAlignment) GUI.Toolbar(rect, (int)hAlign, horizontalAlignment);
            rect.x += rect.width + EditorGUIUtility.standardVerticalSpacing;
            vAlign = (VAlignment) GUI.Toolbar(rect, (int)vAlign, verticalAlignment);
            
            if (EditorGUI.EndChangeCheck())
            {
                prop.enumValueIndex = (int)CombineAxis(hAlign, vAlign);
            }

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static (HAlignment, VAlignment) SeparateAxis(TextAnchor alignment)
        {
            var hAlignment = alignment switch
            {
                TextAnchor.LowerLeft or TextAnchor.MiddleLeft or TextAnchor.UpperLeft => HAlignment.Left,
                TextAnchor.LowerRight or TextAnchor.MiddleRight or TextAnchor.UpperRight => HAlignment.Right,
                _ => HAlignment.Center,
            };

            var vAlignment = alignment switch
            {
                TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => VAlignment.Bottom,
                TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight => VAlignment.Top,
                _ => VAlignment.Center,
            };

            return (hAlignment, vAlignment);
        }
        
        private static TextAnchor CombineAxis(HAlignment hAlignment, VAlignment vAlignment)
        {
            return (hAlignment, vAlignment) switch
            {
                (HAlignment.Left, VAlignment.Bottom) => TextAnchor.LowerLeft,
                (HAlignment.Left, VAlignment.Center) => TextAnchor.MiddleLeft,
                (HAlignment.Left, VAlignment.Top) => TextAnchor.UpperLeft,
                (HAlignment.Center, VAlignment.Bottom) => TextAnchor.LowerCenter,
                (HAlignment.Center, VAlignment.Center) => TextAnchor.MiddleCenter,
                (HAlignment.Center, VAlignment.Top) => TextAnchor.UpperCenter,
                (HAlignment.Right, VAlignment.Bottom) => TextAnchor.LowerRight,
                (HAlignment.Right, VAlignment.Center) => TextAnchor.MiddleRight,
                (HAlignment.Right, VAlignment.Top) => TextAnchor.UpperRight,
                _ => TextAnchor.MiddleCenter,
            };
        }
    }
}