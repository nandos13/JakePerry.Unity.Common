/* TODO: Come back to this implementation.
 * This PropertyDrawer is for fields decorated with [SerializeReference].
 * It draws a type selector rect, allowing the user to specify what object
 * type is to be serialized.
 * Currently, it is not bound to a particular decorator attribute.
 * It also has some other issues. Mainly, the 'label' line is always drawn.
 */

/*
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    internal sealed class SerializeReferenceTypeSelectorDrawer : PropertyDrawer
    {
        private static SerializeReferenceTypeSelectorDrawer _inst;

        internal static SerializeReferenceTypeSelectorDrawer Instance => _inst ??= new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            if (property.isExpanded)
            {
                // Return standard height.
                // Note that this usually includes one line for the label that does not
                // contain any properties. We use this line to display the object type field.
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            else
            {
                // One line to display the object type field
                return LineHeight;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var r = position.WithHeight(LineHeight);
            property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, label, toggleOnLabelClick: true);

            r = r.PadLeft(EditorGUIUtility.labelWidth + 2f);
            position = position.PadTop(r.height + Spacing);

            // TODO: Use SerializedProperty.contentHash to cache shit (will require unity update)

            var resolver = new UnityEditorHelper.SerializedPropertyResolver(property);
            var value = resolver.GetSerializedValue();
            var member = resolver.GetSerializedMember();

            var type = value?.GetType();

            var content = GetTempContent(type?.Name ?? "<null>");

            // TODO: This needs to restrict selectable choices to types that are assignable to the member type
            // and are not abstract
            if (SerializeTypeDefinitionDrawer.DrawTypeSelectRect(r, content, ref type, null))
            {
                object obj = null;
                if (type is not null)
                {
                    obj = RuntimeHelpers.GetUninitializedObject(type);
                }
                resolver.SetSerializedValue(obj);
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                {
                    // TODO: This may need to check for custom drawers like the other code does...
                    EditorGUI.PropertyField(position, property, null, true);
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
*/
