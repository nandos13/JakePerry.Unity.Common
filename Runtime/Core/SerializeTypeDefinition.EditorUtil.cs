#if UNITY_EDITOR
using UnityEditor;

namespace JakePerry.Unity
{
    public partial struct SerializeTypeDefinition
    {
        public static class EditorUtil
        {
            internal static void GetTypeParts(
                SerializedProperty property,
                out SerializedProperty typeName,
                out SerializedProperty genericArgs)
            {
                typeName = property.FindPropertyRelative("m_typeName");
                genericArgs = property.FindPropertyRelative("m_genericArgs");
            }

            public static SerializeTypeDefinition GetTypeDefinition(SerializedProperty property)
            {
                GetTypeParts(property, out SerializedProperty typeName, out SerializedProperty genericArgs);

                var argsCount = genericArgs.arraySize;
                var args = argsCount > 0 ? new SerializeTypeDefinition[argsCount] : null;

                for (int i = 0; i < argsCount; ++i)
                {
                    var argProp = genericArgs.GetArrayElementAtIndex(i);
                    args[i] = GetTypeDefinition(argProp);
                }

                return new SerializeTypeDefinition()
                {
                    m_typeName = typeName.stringValue,
                    m_genericArgs = args
                };
            }

            public static void SetTypeDefinition(SerializedProperty property, SerializeTypeDefinition value)
            {
                GetTypeParts(property, out SerializedProperty typeName, out SerializedProperty genericArgs);

                typeName.stringValue = value.m_typeName;

                var argsCount = value.m_genericArgs?.Length ?? 0;
                genericArgs.arraySize = argsCount;

                for (int i = 0; i < argsCount; ++i)
                {
                    var argProp = genericArgs.GetArrayElementAtIndex(i);
                    SetTypeDefinition(argProp, value.m_genericArgs[i]);
                }
            }
        }
    }
}
#endif // UNITY_EDITOR
