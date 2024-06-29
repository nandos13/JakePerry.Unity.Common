using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(LinkableData<>))]
    public sealed class LinkableDataDrawer : PropertyDrawer
    {
        // TODO: Get rid of this if we dont end up using it (see commented section below.
        /*
        [System.Serializable]
        private sealed class DummyDataHolder<T> : UnityEngine.Object
        {
            public T data;
        }
        */

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var linkedProp = property.FindPropertyRelative("m_linked");

            float height = LineHeight;

            if (linkedProp.boolValue)
            {
                height += Spacing + LineHeight;

                var serializedMember = PropertyPathWalker.GetFieldOrProperty(property);
                var genericArg = serializedMember.MemberType.GenericTypeArguments[0];

                var sourceProp = property.FindPropertyRelative("m_source");

                var interfaceType = ReflectionEx.MakeGenericType(typeof(IValueFromUnityObject<>), new(genericArg));
                var @interface = InspectableInterface.EditorUtil.GetReferencedInterface(sourceProp, interfaceType);

                if (@interface is null)
                {
                    height += Spacing + LineHeight;
                }
                else
                {
                    var targetProp = InspectableInterface.EditorUtil.GetTargetObjectProperty(sourceProp);
                    height += Spacing + EditorGUI.GetPropertyHeight(targetProp, true);
                }
            }
            else
            {
                var valueProp = property.FindPropertyRelative("m_value");
                height += Spacing + EditorGUI.GetPropertyHeight(valueProp, true);
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var modeRect = position.WithHeight(LineHeight);
            position = position.PadTop(LineHeight + Spacing);

            // TODO: Use a nicer toggle
            var linkedProp = property.FindPropertyRelative("m_linked");

            // Draw a toggle to switch between linking data and serializing in place
            linkedProp.boolValue = EditorGUI.Toggle(modeRect, "Linked", linkedProp.boolValue);

            ++EditorGUI.indentLevel;

            if (linkedProp.boolValue)
            {
                // Attain a MemberInfo to the serialized property via reflection
                var serializedMember = PropertyPathWalker.GetFieldOrProperty(property);

                // Find the generic argument which indicates the desired data type
                var genericArg = serializedMember.MemberType.GenericTypeArguments[0];

                var sourceProp = property.FindPropertyRelative("m_source");

                var prodiverRect = position.WithHeight(LineHeight);
                position = position.PadTop(LineHeight + Spacing);

                EditorGUI.PropertyField(prodiverRect, sourceProp, true);

                var interfaceType = ReflectionEx.MakeGenericType(typeof(IValueFromUnityObject<>), new(genericArg));
                var @interface = InspectableInterface.EditorUtil.GetReferencedInterface(sourceProp, interfaceType);

                // TODO: Needs to be a button somewhere to break the link and just serialize
                // the current linked value in place (m_value)

                if (@interface is null)
                {
                    // When the interface is unassigned, simply display a hint to the user
                    var noTargetHintRect = position.WithHeight(LineHeight);
                    EditorGUI.LabelField(
                        noTargetHintRect,
                        "No data source object assigned",
                        EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.HelpBox(position, "Data preview WIP", MessageType.Info);

                        // TODO: Need to create a dummy obj to hold the linked data and draw it.
                        // Activator.CreateInstance doesnt work because it doesnt alloc unmanaged, no instance id.
                        // ScriptableObject doesnt work, i think because generic type.
                        // This might be a dead end, in which case the IValueFromUnityObject
                        // interface is not good enough and I'll instead need to consider serializing
                        // property paths, per my comment left at the top of LinkableData.cs...

                        --EditorGUI.indentLevel;
                        return;

                        //var dummyType = ReflectionEx.MakeGenericType(typeof(DummyDataHolder<>), new(genericArg));
                        //var dummy = ScriptableObject.CreateInstance(dummyType);
                        //var dummy2 = Activator.CreateInstance(dummyType, true);
                        //var dummy = (UnityEngine.Object)dummy2;
                        //Debug.LogError($"Made dummy {dummy.GetType()}");
                        //Debug.LogError($"Did cast: {dummy is not null}");
                        //dummy.GetInstanceID();
                        //
                        //var sObj = new SerializedObject(dummy, property.serializedObject.context);
                        //var dummyProp = sObj.FindProperty("data");
                        //
                        //EditorGUI.PropertyField(position, dummyProp, true);
                    }
                }
            }
            else
            {
                var valueProp = property.FindPropertyRelative("m_value");
                EditorGUI.PropertyField(position, valueProp, true);
            }

            --EditorGUI.indentLevel;
        }
    }
}
