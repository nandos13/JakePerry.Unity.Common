using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    internal static class ReturnDelegatesEditorUtil
    {
        internal enum PropertyMethodType { None, Get, Set }

        private static readonly GUIContent[] _editorInvocationOptions = new GUIContent[3]
        {
            new GUIContent(
                "Return Default Value",
                "Delegate is not invoked in Edit mode. Instead, the default value is returned."),
            new GUIContent(
                "Return Mock Value",
                "Delegate is not invoked in Edit mode. Instead, a mock value is returned."),
            new GUIContent(
                "Invoke Delegate",
                "Delegate is invoked as normal in Edit mode.")
        };

        internal static GUIContent[] EditorInvocationOptions => _editorInvocationOptions;

        internal static int CompareMemberDisplayOrder(MemberInfo x, MemberInfo y)
        {
            bool xIsProperty = x is PropertyInfo;
            bool yIsProperty = y is PropertyInfo;

            int comp = yIsProperty.CompareTo(xIsProperty);
            if (comp == 0)
            {
                comp = StringComparer.Ordinal.Compare(x.Name, y.Name);
            }

            return comp;
        }

        internal static string GetNiceTypeName(Type type)
        {
            var compilerAlias = CompilerAliases.GetAlias(type);
            if (compilerAlias is not null)
            {
                return compilerAlias;
            }

            if (type == typeof(UnityEngine.Object))
            {
                return "UnityEngine.Object";
            }

            return type.Name;
        }

        internal static void GetArgumentString<T>(T argTypes, StringBuilder sb)
            where T : IEnumerable<Type>
        {
            sb.Append('(');

            bool flag = false;
            foreach (var t in argTypes)
            {
                if (flag) sb.Append(", ");
                sb.Append(GetNiceTypeName(t));

                flag = true;
            }

            sb.Append(')');
        }

        internal static string GetNicePropertyString(PropertyInfo p, bool includeReturnType, PropertyMethodType targetMethod)
        {
            var sb = StringBuilderCache.Acquire();

            if (includeReturnType)
            {
                sb.Append(GetNiceTypeName(p.PropertyType));
                sb.Append(' ');
            }

            sb.Append(p.Name);
            if (targetMethod == PropertyMethodType.Get)
            {
                sb.Append(" { get; }");
            }
            else if (targetMethod == PropertyMethodType.Set)
            {
                sb.Append(" { set; }");
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal static string GetNiceMethodString(MethodInfo m, bool includeReturnType)
        {
            if (!includeReturnType) return m.Name;

            var paramTypes = System.Linq.Enumerable.Select(m.GetParameters(), p => p.ParameterType);

            var sb = StringBuilderCache.Acquire();

            sb.Append(GetNiceTypeName(m.ReturnType));
            sb.Append(' ');
            sb.Append(m.Name);
            GetArgumentString(paramTypes, sb);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal static bool IsMemberParameterSerializable(Type t)
        {
            // These types are supported out of the box.
            if (t == typeof(int) ||
                t == typeof(float) ||
                t == typeof(bool) ||
                t == typeof(string) ||
                t == typeof(UnityEngine.Object))
            {
                return true;
            }

            return SerializableMethodArgument.EditorUtil.FindContainerForType(t) != null;
        }
    }
}
