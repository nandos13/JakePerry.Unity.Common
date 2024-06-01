using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(Color24))]
    public sealed class Color24Drawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var r = property.FindPropertyRelative("r");
            var g = property.FindPropertyRelative("g");
            var b = property.FindPropertyRelative("b");

            var c32 = new Color32((byte)r.intValue, (byte)g.intValue, (byte)b.intValue, 255);

            EditorGUI.BeginChangeCheck();

            var c = EditorGUI.ColorField(position, label, c32, showEyedropper: true, showAlpha: false, hdr: false);

            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                c32 = (Color32)c;
                r.intValue = c32.r;
                g.intValue = c32.g;
                b.intValue = c32.b;
            }
        }
    }
}
