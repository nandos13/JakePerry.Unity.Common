using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity.Events
{
    [CustomPropertyDrawer(typeof(ExtendedInvocationArgument))]
    internal sealed class ExtendedInvocationArgumentDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var v = property.FindPropertyRelative("m_arg.m_value");
            if (v != null)
            {
                return EditorGUI.GetPropertyHeight(v, includeChildren: true);
            }

            return LineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var v = property.FindPropertyRelative("m_arg.m_value");
            if (v != null)
            {
                EditorGUI.PropertyField(position, v, includeChildren: true);
            }
            else
            {
                // TODO: Better message here, make it clear to the user what is wrong.
                EditorGUI.LabelField(position, "Couldn't find m_value field.");
            }
        }
    }
}
