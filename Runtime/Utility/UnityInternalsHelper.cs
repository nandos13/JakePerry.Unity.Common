using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JakePerry.Unity
{
    // TODO: This isn't really related to Unity (only thing is unity version code printed in error logs).
    // Consider making this a ReflectionCache, etc & moving it to the JakePerry.Common project.

    /// <summary>
    /// Helper methods used to gain access to Unity's internal code.
    /// </summary>
    internal static class UnityInternalsHelper
    {
        private readonly struct TypeKey
        {
            public readonly Assembly assembly;
            public readonly string typeName;

            public TypeKey(Assembly a, string n) { assembly = a; typeName = n; }
        }

        private readonly struct FieldPropertyKey
        {
            public readonly Type type;
            public readonly string name;

            public FieldPropertyKey(Type t, string n) { type = t; name = n; }
        }

        private readonly struct MethodKey
        {
            public readonly Type type;
            public readonly string methodName;
            public readonly ParamsArray<Type> types;

            public MethodKey(
                Type type,
                string methodName,
                ParamsArray<Type> types = default)
            {
                this.type = type;
                this.methodName = methodName;
                this.types = types;
            }
        }

        private static readonly Dictionary<TypeKey, Type> _typeLookup = new();

        private static readonly Dictionary<FieldPropertyKey, ValueMemberInfo> _valueMemberLookup = new();

        private static readonly Dictionary<MethodKey, MethodInfo> _methodLookup = new();

        /// <summary>
        /// Get access to a type that is not publicly exposed.
        /// </summary>
        /// <param name="assembly">The assembly which defines the type.</param>
        /// <param name="typeName">The full name of the type.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        internal static Type GetType(Assembly assembly, string typeName)
        {
            _ = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _ = typeName ?? throw new ArgumentNullException(nameof(typeName));

            if (typeName.Length == 0) throw new ArgumentException("Empty string.", nameof(typeName));

            var key = new TypeKey(assembly, typeName);
            if (!_typeLookup.TryGetValue(key, out Type type))
            {
                type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);

                if (type is null)
                {
                    Debug.LogError($"Unable to find internal type {typeName}! Unity version: {Application.unityVersion}");
                }

                _typeLookup[key] = type;
            }

            return type;
        }

        internal static ValueMemberInfo GetField(
            Type type,
            string fieldName,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = fieldName ?? throw new ArgumentNullException(nameof(fieldName));

            if (fieldName.Length == 0) throw new ArgumentException("Empty string.", nameof(fieldName));

            var key = new FieldPropertyKey(type, fieldName);
            if (!_valueMemberLookup.TryGetValue(key, out ValueMemberInfo member))
            {
                var field = type.GetField(fieldName, flags);
                member = new ValueMemberInfo(field);

                if (field is null)
                {
                    Debug.LogError($"Unable to find internal field {fieldName} for declaring type {type}! Unity version: {Application.unityVersion}");
                }

                _valueMemberLookup[key] = member;
            }

            return member;
        }

        internal static ValueMemberInfo GetProperty(
            Type type,
            string propertyName,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = propertyName ?? throw new ArgumentNullException(nameof(propertyName));

            if (propertyName.Length == 0) throw new ArgumentException("Empty string.", nameof(propertyName));

            var key = new FieldPropertyKey(type, propertyName);
            if (!_valueMemberLookup.TryGetValue(key, out ValueMemberInfo member))
            {
                var property = type.GetProperty(propertyName, flags);
                member = new ValueMemberInfo(property);

                if (property is null)
                {
                    Debug.LogError($"Unable to find internal property {propertyName} for declaring type {type}! Unity version: {Application.unityVersion}");
                }

                _valueMemberLookup[key] = member;
            }

            return member;
        }

        internal static MethodInfo GetMethod(
            Type type,
            string methodName,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public,
            ParamsArray<Type> types = default)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

            if (methodName.Length == 0) throw new ArgumentException("Empty string.", nameof(methodName));

            var key = new MethodKey(type, methodName, types: types);
            if (!_methodLookup.TryGetValue(key, out MethodInfo method))
            {
                if (types.Length == 0)
                {
                    method = type.GetMethod(name: methodName, bindingAttr: flags);
                }
                else
                {
                    var typesArray = types.ToArray();

                    method = type.GetMethod(
                        name: methodName,
                        bindingAttr: flags,
                        binder: null,
                        callConvention: default,
                        types: typesArray,
                        modifiers: null);
                }

                if (method is null)
                {
                    Debug.LogError($"Unable to find internal method {methodName} for declaring type {type}! Unity version: {Application.unityVersion}");
                }

                _methodLookup[key] = method;
            }

            return method;
        }
    }
}
