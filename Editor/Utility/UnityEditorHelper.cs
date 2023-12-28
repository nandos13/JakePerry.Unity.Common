using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class UnityEditorHelper
    {
        public sealed class SerializedPropertyResolver
        {
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

            private readonly SerializedObject m_rootObj;
            private readonly string m_propertyPath;
            private readonly Stack<Capture> m_captureStack;

            public SerializedPropertyResolver(SerializedProperty property)
            {
                // Get the object that contains the serialized member
                m_rootObj = property.serializedObject;
                m_propertyPath = property.propertyPath;

                m_captureStack = ResolveStack(m_propertyPath);
            }

            private static ValueMemberInfo GetMemberFromType(Type type, string memberName)
            {
                const MemberTypes kMemberFlags = MemberTypes.Field | MemberTypes.Property;
                const BindingFlags kBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                const BindingFlags kBaseTypeBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

                var originalType = type;

                var members = type.GetMember(memberName, kMemberFlags, kBindingFlags);

                // If no members are found, search up the type hierarchy for a matching private member
                while ((members?.Length ?? 0) == 0)
                {
                    type = type.BaseType;

                    if (type is null)
                        break;

                    members = type.GetMember(memberName, kMemberFlags, kBaseTypeBindingFlags);
                }

                // Throw an exception if the member is not found (should never happen)
                if ((members?.Length ?? 0) == 0)
                {
                    throw new Exception($"Failed to find the serialized member with name '{memberName}' from type {originalType} via reflection.");
                }

                // Not sure if this could ever happen if we're only specifying the Field | Property flags, but detect this just in case...
                if (members.Length != 1)
                {
                    throw new NotImplementedException("More than one member was found. This code will require some changes to discern the correct member.");
                }

                return ValueMemberInfo.FromMemberInfo(members[0]);
            }

            private Stack<Capture> ResolveStack(string path)
            {
                // Unity's property path breaks array/list elements down weirdly, it's easier to deal with if we combine them.
                path = path.Replace(".Array.data[", ".__Array[");

                // This property may be nested several classes/structs deep. In this case,
                // the string returned by 'propertyPath' will split each respective member name
                // via the period character.
                var relativePath = path.IndexOf('.') > -1
                    ? path.Split(new char[] { '.' })
                    : new string[1] { path };

                var stack = new Stack<Capture>();

                object nextTarget = m_rootObj.targetObject;
                var declaringType = nextTarget.GetType();

                // Iterate all segments except the last
                for (int i = 0; i < relativePath.Length; i++)
                {
                    var target = nextTarget;

                    var memberName = relativePath[i];

                    // Special case: Handle cases where we're attempting to access items in an array or List<T>
                    if (memberName.StartsWith("__Array["))
                    {
                        Debug.Assert(i > 0);

                        var numberStr = memberName.Substring(8, memberName.Length - 9);
                        var index = int.Parse(numberStr);

                        var lastCapture = stack.Peek();

                        Type elementType;

                        if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var itemsMember = GetMemberFromType(declaringType, "_items");
                            var listObj = lastCapture.value;
                            var listInternalArray = itemsMember.GetValue(listObj);

                            stack.Push(new Capture(itemsMember, listObj, listInternalArray));
                            lastCapture = stack.Peek();

                            elementType = listObj.GetType().GetGenericArguments()[0];
                        }
                        else if (declaringType.IsArray)
                        {
                            if (declaringType == typeof(Array))
                                throw new NotImplementedException("The Array class itself is not supported yet. Will require a refactor to use a MemberInfo instead of a field/property so that we can correctly set the value (Array does not provide an indexer property).");

                            elementType = declaringType.GetElementType();
                        }
                        else
                        {
                            // Should never happen, but needs further investigation if it does later.
                            throw new NotImplementedException($"Failed to resolve declaring type!");
                        }

                        var array = (Array)lastCapture.value;
                        var element = array.GetValue(index);

                        stack.Push(new Capture(default, array, element, propertyIndex: index));

                        declaringType = elementType;
                        nextTarget = element;
                    }
                    // Handle regular members
                    else
                    {
                        var member = GetMemberFromType(declaringType, memberName);
                        var value = member.GetValue(target);

                        stack.Push(new Capture(member, target, value));

                        declaringType = member.MemberType;
                        nextTarget = value;
                    }
                }

                return stack;
            }

            /// <returns>
            /// A <see cref="ValueMemberInfo"/> representation of the member targeted by the given serialized property.
            /// </returns>
            public ValueMemberInfo GetSerializedMember()
            {
                foreach (var c in m_captureStack)
                    if (!c.member.IsNull)
                    {
                        return c.member;
                    }

                Debug.LogError($"Failed to resolve any members from property path {m_propertyPath}");
                return default;
            }

            public object GetSerializedValue()
            {
                return m_captureStack.Peek().value;
            }

            public void SetSerializedValue(object value)
            {
                bool setNext = true;
                foreach (var c in m_captureStack)
                {
                    if (setNext)
                    {
                        if (c.propertyIndex > -1)
                            ((Array)c.target).SetValue(value, c.propertyIndex);
                        else
                            c.member.SetValue(c.target, value);
                    }

                    setNext = c.target != null && c.target.GetType().IsValueType;
                    if (setNext)
                        value = c.target;
                }

                EditorUtility.SetDirty(m_rootObj.targetObject);
            }
        }

        public static Texture2D GetMessageIcon(MessageType messageType)
        {
            var method = typeof(EditorGUIUtility).GetMethod("GetHelpIcon", BindingFlags.Static | BindingFlags.NonPublic);
            var result = method.Invoke(null, new object[] { messageType });

            return (Texture2D)result;
        }

        /// <summary>
        /// Helper method to safely get the path for an asset corresponding to the given guid.
        /// </summary>
        /// <param name="guid">Guid of a project asset.</param>
        /// <param name="assetPath">Path of the asset relative to the project folder.</param>
        /// <returns>
        /// <see langword="true"/> if an asset was found; Otherwise, <see langword="false"/>.
        /// </returns>
        /// <seealso cref="AssetDatabase.GUIDToAssetPath(string)"/>
        public static bool TryGetAssetPath(SerializeGuid guid, out string assetPath)
        {
            var guidString = guid.UnityGuidString;
            assetPath = AssetDatabase.GUIDToAssetPath(guidString);

            return !string.IsNullOrEmpty(assetPath);
        }

        /// <summary>
        /// Safely get a project asset of the given type with the given guid.
        /// </summary>
        /// <typeparam name="T">Asset type.</typeparam>
        /// <param name="asset">The project asset corresponding to the guid.</param>
        /// <inheritdoc cref="TryGetAssetPath(SerializeGuid, out string)"/>
        public static bool TryGetProjectAsset<T>(SerializeGuid guid, out T asset)
            where T : UnityEngine.Object
        {
            asset = null;
            if (TryGetAssetPath(guid, out string assetPath))
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
            return asset != null;
        }

        /// <inheritdoc cref="TryGetProjectAsset{T}(SerializeGuid, out T)"/>
        public static bool TryGetProjectAsset(SerializeGuid guid, out UnityEngine.Object asset)
        {
            return TryGetProjectAsset<UnityEngine.Object>(guid, out asset);
        }

        /// <inheritdoc cref="ResourcesEx.IsResourcesPath(string)"/>
        public static bool IsResourcesPath(string path)
        {
            return ResourcesEx.IsResourcesPath(path);
        }

        /// <summary>
        /// Attempts to find the Resources-relative path for an arbitrary asset located
        /// at the given path within the Asset Database.
        /// </summary>
        /// <param name="path">
        /// A file path relative to the project.
        /// </param>
        /// <param name="resourcePath">
        /// The corresponding load path relative to the Resources folder of the asset,
        /// or an empty string if it could not be found.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the asset was found and exists within a Resources
        /// folder; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryGetResourcesPath(string path, out string resourcePath)
        {
            return ResourcesEx.TryGetResourcesPath(path, out resourcePath);
        }

        /// <summary>
        /// Attempts to find the Resources-relative path for an asset with the given guid.
        /// </summary>
        /// <param name="guid">Guid of the resource asset.</param>
        /// <inheritdoc cref="TryGetResourcesPath(string, out string)"/>
        public static bool TryGetResourcesPath(SerializeGuid guid, out string resourcePath)
        {
            resourcePath = string.Empty;
            return TryGetAssetPath(guid, out string assetPath)
                && TryGetResourcesPath(assetPath, out resourcePath);
        }

        public static object GetSerializedValue(SerializedProperty property)
        {
            return new SerializedPropertyResolver(property).GetSerializedValue();
        }

        public static void SetSerializedValue(SerializedProperty property, object value)
        {
            new SerializedPropertyResolver(property).SetSerializedValue(value);
        }

        /// <inheritdoc cref="SerializedPropertyResolver.GetSerializedMember"/>
        public static ValueMemberInfo GetSerializedMember(SerializedProperty property)
        {
            return new SerializedPropertyResolver(property).GetSerializedMember();
        }
    }
}
