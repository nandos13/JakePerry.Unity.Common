#if UNITY_EDITOR
using System.Text;
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
                out SerializedProperty wantsUnboundGeneric,
                out SerializedProperty genericArgs)
            {
                typeName = property.FindPropertyRelative("m_typeName");
                wantsUnboundGeneric = property.FindPropertyRelative("m_wantsUnboundGeneric");
                genericArgs = property.FindPropertyRelative("m_genericArgs");
            }

            public static SerializeTypeDefinition GetTypeDefinition(SerializedProperty property)
            {
                GetTypeParts(
                    property,
                    out SerializedProperty typeName,
                    out SerializedProperty wantsUnboundGeneric,
                    out SerializedProperty genericArgs);

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
                    m_wantsUnboundGeneric = wantsUnboundGeneric.boolValue,
                    m_genericArgs = args
                };
            }

            public static void SetTypeDefinition(SerializedProperty property, SerializeTypeDefinition value)
            {
                GetTypeParts(
                    property,
                    out SerializedProperty typeName,
                    out SerializedProperty wantsUnboundGeneric,
                    out SerializedProperty genericArgs);

                typeName.stringValue = value.m_typeName;
                wantsUnboundGeneric.boolValue = value.m_wantsUnboundGeneric;

                if (!value.m_wantsUnboundGeneric)
                {
                    var argsCount = value.m_genericArgs?.Length ?? 0;
                    genericArgs.arraySize = argsCount;

                    for (int i = 0; i < argsCount; ++i)
                    {
                        var argProp = genericArgs.GetArrayElementAtIndex(i);
                        SetTypeDefinition(argProp, value.m_genericArgs[i]);
                    }
                }
                else
                {
                    genericArgs.arraySize = 0;
                }
            }

            // TODO: Remove this and replace with a better method that
            // takes the serialized property, handles making the label, handles colors,
            // hover state over generic args, etc.
            private static bool GetTypeName(SerializeTypeDefinition typeDef, StringBuilder sb)
            {
                var t = typeDef.ResolveTypeUnbound(throwOnError: false);
                if (t is null)
                {
                    return false;
                }

                var name = t.Name;
                if (t.IsGenericTypeDefinition)
                {
                    sb.Append(name, 0, name.LastIndexOf('`'));

                    sb.Append('<');

                    var args = t.GetGenericArguments();
                    if (typeDef.m_wantsUnboundGeneric)
                    {
                        for (int i = 0; i < args.Length; ++i)
                        {
                            if (i > 0) sb.Append(", ");
                            sb.Append(args[i].Name);
                        }
                    }
                    else
                    {
                        var args2 = typeDef.m_genericArgs;
                        for (int i = 0; i < args2.Length; ++i)
                        {
                            if (i > 0) sb.Append(", ");
                            if (!GetTypeName(args2[i], sb))
                            {
                                sb.Append(args[i].Name);
                            }
                        }
                    }

                    sb.Append('>');
                }
                else
                {
                    sb.Append(name);
                }

                return true;
            }

            public static string GetTypeName(SerializeTypeDefinition typeDef)
            {
                var sb = StringBuilderCache.Acquire();
                GetTypeName(typeDef, sb);

                return StringBuilderCache.GetStringAndRelease(ref sb);
            }
        }
    }
}
#endif // UNITY_EDITOR
