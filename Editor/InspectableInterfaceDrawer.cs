using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(InspectableInterface<>))]
    public class InspectableInterfaceDrawer : PropertyDrawer
    {
        private static Type GetGenericType(ValueMemberInfo member)
        {
            Type memberType = member.MemberType;

            if (memberType.IsGenericType)
            {
                return memberType.GenericTypeArguments[0];
            }

            throw new InvalidOperationException("The resolved MemberInfo's field or property type is not a generic type.");
        }

        private static bool IsIncorrectType(UnityEngine.Object obj, Type typeRestriction)
        {
            return obj != null && !typeRestriction.IsAssignableFrom(obj.GetType());
        }

        private void DrawErrorHint(ref Rect contentRect)
        {
            const float kPadding = 4;

            // Create a squared rect for the error hint
            var size = contentRect.height;
            var errorIconRect = new Rect(contentRect.x, contentRect.y, size, size);

            // Shrink the original content rect
            contentRect.x += size + kPadding;
            contentRect.width -= size + kPadding;

            // Draw the error hint
            var icon = UnityEditorHelper.GetMessageIcon(MessageType.Error);
            var content = new GUIContent(icon, "Current underlying Object reference does not implement the target interface.");
            EditorGUI.LabelField(errorIconRect, content);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Attain a MemberInfo to the serialized property via reflection
            var serializedMember = UnityEditorHelper.GetSerializedMember(property);

            // Find the generic argument which indicates the desired interface type
            var typeRestriction = GetGenericType(serializedMember);

            // Get the property for the underlying UnityEngine.Object reference field
            var targetProperty = property.FindPropertyRelative("m_targetObject");

            // Draw the prefix label & get remaining rect for content
            var contentRect = EditorGUI.PrefixLabel(position, label);

            var target = targetProperty.objectReferenceValue;

            if (IsIncorrectType(target, typeRestriction))
            {
                DrawErrorHint(ref contentRect);
            }

            // Draw the object field & detect when the value is changed by user
            UnityEngine.Object newTarget;
            EditorGUI.BeginChangeCheck();
            {
                newTarget = EditorGUI.ObjectField(contentRect, target, typeRestriction, allowSceneObjects: true);
            }
            bool didChange = EditorGUI.EndChangeCheck();

            if (didChange)
            {
                bool setNewTarget = true;

                // Ensure we don't allow the user to change to an object that does not implement the desired interface
                if (IsIncorrectType(newTarget, typeRestriction))
                {
                    setNewTarget = false;

                    Debug.LogError($"The selected object does not implement the type restriction interface {typeRestriction}.");
                }

                if (setNewTarget)
                {
                    targetProperty.objectReferenceValue = newTarget;
                }
            }
        }
    }
}
