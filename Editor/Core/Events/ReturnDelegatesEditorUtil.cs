using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    internal static class ReturnDelegatesEditorUtil
    {
        internal enum PropertyMethodType { None, Get, Set }

        private static readonly GUIContent[] _policyOptions = new GUIContent[4]
        {
            new GUIContent(
                "Global (Default)",
                "Use global error handling policy."),
            new GUIContent(
                "None",
                "Ignore errors. Invocation does not proceed, and the default value is returned."),
            new GUIContent(
                "Log Error",
                "Log an error. Invocation does not proceed, and the default value is returned."),
            new GUIContent(
                "Log Exception",
                "An exception of type " + nameof(InvocationTargetDestroyedException) + " is thrown")
        };

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

        internal static GUIContent[] PolicyOptions => _policyOptions;

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

        // TODO: Share TypeSelector._builtInTypes, put it in a util class etc.
        internal static string GetNiceTypeName(Type type)
        {
            if (type == typeof(bool)) return "bool";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(char)) return "char";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(short)) return "short";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(nint)) return "nint";
            if (type == typeof(nuint)) return "nuint";
            if (type == typeof(string)) return "string";
            if (type == typeof(object)) return "object";
            if (type == typeof(UnityEngine.Object)) return "UnityEngine.Object";
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
    }
}
