using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace JakePerry.Unity
{
    /// <summary>
    /// Utility for walking a property path via reflection.
    /// <para/>
    /// A property path is a string which describes a sequence of fields and/or properties
    /// which can be traversed, starting from a root object, to obtain a desired member at
    /// the end of the path.
    /// In the editor, Unity uses such an approach to expose serialized data via the
    /// SerializedProperty type.
    /// <para/>
    /// A property path can describe the accessing of an element in an array or list by using
    /// the special identifier <b>Array.data[X]</b> where X is the index integer.
    /// This matches the format of property paths returned by Unity's SerializedProperty.
    /// </summary>
    public static class PropertyPathWalker
    {
        // SerializedProperty's propertyPath represents arrays & lists like so...
        // eg. "items.Array.data[3]" represents the 3rd element in an array/list named "items".
        private const string kArrayExpression = "Array.data[";
        private const int kArrayExprLen = 11; // kArrayExpression.Length

        private readonly struct Capture
        {
            public readonly ValueMemberInfo member;
            public readonly int propertyIndex;
            public readonly object target;
            public readonly object value;

            public Capture(ValueMemberInfo member, object target, object value, int propertyIndex = -1)
            {
                this.member = member;
                this.propertyIndex = propertyIndex;
                this.target = target;
                this.value = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckArguments(object rootObject, string path)
        {
            _ = rootObject ?? throw new ArgumentNullException(nameof(rootObject));
            _ = path ?? throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
            {
                throw new ArgumentException("String is empty.", nameof(path));
            }
        }

        private static ValueMemberInfo GetMember(Type type, Substring memberName)
        {
            const BindingFlags kBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags kBaseTypeBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            var originalType = type;

            var member = ReflectionEx.GetFieldOrProperty(type, memberName, kBindingFlags, false);

            // If no member is found, search up the type hierarchy for a matching private member
            while (member.IsNull)
            {
                type = type.BaseType;

                if (type is null)
                    break;

                member = ReflectionEx.GetFieldOrProperty(type, memberName, kBaseTypeBindingFlags, false);
            }

            // Throw an exception if the member is not found
            if (member.IsNull)
            {
                throw new InvalidOperationException(
                    $"Failed to find the member with name '{memberName}' from type {originalType} via reflection.");
            }

            return member;
        }

        /// <summary>
        /// Get a substring representing the next member in the property path.
        /// </summary>
        /// <param name="path">A property path.</param>
        /// <param name="start">Segment start index.</param>
        private static Substring GetPathSegment(string path, int start)
        {
            int end;

            // If the current member in the path matches the expression for an array/list,
            // skip the period character included in the array expression.
            if (path.Length >= start + kArrayExprLen &&
                path.AsSpan(start, kArrayExprLen).Equals(kArrayExpression, StringComparison.Ordinal))
            {
                end = path.IndexOf('.', start + kArrayExprLen);
            }
            // Otherwise, the current member name is simply delimeted by the next period character.
            else
            {
                end = path.IndexOf('.', start);
            }

            if (end < 0) end = path.Length;

            int count = end - start;

            return count > 0
                ? new Substring(path, start, count)
                : Substring.Empty;
        }

        /// <summary>
        /// Get the target array object for array accessing operation.
        /// <para/>
        /// If the target object is a List, this method will get its internal array.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        private static Array GetArray(object target)
        {
            var t = target.GetType();
            if (t.IsArray)
            {
                return (Array)target;
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Access the list's internal array
                var itemsMember = GetMember(t, "_items");
                return (Array)itemsMember.GetValue(target);
            }

            throw new NotSupportedException("Expected array or list type.");
        }

        private static void ResolveStack(object rootObject, string path, List<Capture> list)
        {
            int start = 0;

            object target = rootObject;
            Type declaringType = target.GetType();

            while (start < path.Length)
            {
                if (target is null)
                {
                    var pathSoFar = path.Substring(0, start - 1);
                    throw new InvalidOperationException($"Null value was returned at path {pathSoFar}. The remainder of the property path cannot be resolved.");
                }

                var segment = GetPathSegment(path, start);

                if (segment.Length == 0)
                {
                    throw new FormatException($"Error at char offset {segment.StartIndex}");
                }

                start += segment.Length + 1;

                // Special case: Handle cases where we're attempting to access items in an array or List<T>
                if (segment.StartsWith(kArrayExpression, StringComparison.Ordinal))
                {
                    const NumberStyles kNumStyle = NumberStyles.Integer;

                    int numLength = segment.Length - kArrayExprLen - 1;
                    if (numLength < 1 ||
                        !segment.EndsWith(']') ||
                        !int.TryParse(segment.AsSpan(kArrayExprLen, numLength), kNumStyle, null, out int index))
                    {
                        throw new FormatException($"Error at char offset {segment.StartIndex + kArrayExprLen}");
                    }

                    var array = GetArray(target);
                    var item = array.GetValue(index);

                    list.Add(new Capture(default, target, item, propertyIndex: index));

                    declaringType = array.GetType().GetElementType();
                    target = item;
                }
                // Handle regular members
                else
                {
                    var member = GetMember(declaringType, segment);
                    var value = member.GetValue(target);

                    list.Add(new Capture(member, target, value));

                    declaringType = member.MemberType;
                    target = value;
                }
            }
        }

        /// <summary>
        /// Finds the field or property at the given property path.
        /// </summary>
        /// <param name="rootObject">Root object instance.</param>
        /// <param name="path">The property path to query.</param>
        /// <returns>
        /// A <see cref="ValueMemberInfo"/> representation of the field or property
        /// targeted by the given property.
        /// </returns>
        public static ValueMemberInfo GetFieldOrProperty(object rootObject, string path)
        {
            CheckArguments(rootObject, path);

            using var scope = ListPool.RentInScope(out List<Capture> list);
            ResolveStack(rootObject, path, list);

            int upperBound = list.Count - 1;
            var capture = list[upperBound];

            if (capture.propertyIndex > -1)
            {
                capture = list[upperBound - 1];
            }

            return capture.member;
        }

        /// <summary>
        /// Finds the current value of the field or property at the given property path.
        /// </summary>
        /// <returns>
        /// The current value stored at the path.
        /// </returns>
        /// <inheritdoc cref="GetFieldOrProperty(object, string)"/>
        public static object GetValue(object rootObject, string path)
        {
            CheckArguments(rootObject, path);

            using var scope = ListPool.RentInScope(out List<Capture> list);
            ResolveStack(rootObject, path, list);

            return list[^1].value;
        }

        /// <summary>
        /// Finds the field or property at the given property path and sets its value.
        /// </summary>
        /// <inheritdoc cref="GetFieldOrProperty(object, string)"/>
        /// <param name="value">The value to be set.</param>
        public static void SetValue(object rootObject, string path, object value)
        {
            CheckArguments(rootObject, path);

            using var scope = ListPool.RentInScope(out List<Capture> list);
            ResolveStack(rootObject, path, list);

            for (int i = list.Count - 1; i >= 0; --i)
            {
                var c = list[i];

                if (c.propertyIndex > -1)
                {
                    var array = GetArray(c.target);
                    array.SetValue(value, c.propertyIndex);
                }
                else
                {
                    c.member.SetValue(c.target, value);
                }

                bool setNext = c.target?.GetType().IsValueType ?? false;
                if (!setNext) break;

                value = c.target;
            }
        }

#if UNITY_EDITOR

        /// <inheritdoc cref="GetFieldOrProperty(object, string)"/>
        public static ValueMemberInfo GetFieldOrProperty(SerializedProperty property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            var rootObject = property.serializedObject.targetObject;
            var path = property.propertyPath;

            return GetFieldOrProperty(rootObject, path);
        }

        /// <inheritdoc cref="GetValue(object, string)"/>
        public static object GetValue(SerializedProperty property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            var rootObject = property.serializedObject.targetObject;
            var path = property.propertyPath;

            return GetValue(rootObject, path);
        }

        /// <inheritdoc cref="SetValue(object, string)"/>
        /// <param name="setDirty">
        /// Indicates whether <see cref="EditorUtility.SetDirty(UnityEngine.Object)"/> should
        /// be invoked on the root object. Defaults to <see langword="true"/>.
        /// </param>
        public static void SetValue(SerializedProperty property, object value, bool setDirty)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            var rootObject = property.serializedObject.targetObject;
            var path = property.propertyPath;

            SetValue(rootObject, path, value);

            if (setDirty)
            {
                EditorUtility.SetDirty(rootObject);
            }
        }

        /// <inheritdoc cref="SetValue(SerializedProperty, object, bool)"/>
        public static void SetValue(SerializedProperty property, object value)
        {
            SetValue(property, value, true);
        }

#endif // UNITY_EDITOR
    }
}
