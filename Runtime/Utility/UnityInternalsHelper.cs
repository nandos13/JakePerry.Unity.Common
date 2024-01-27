using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Helper methods used to gain access to Unity's internal code.
    /// </summary>
    internal static class UnityInternalsHelper
    {
        private readonly struct TypeKey
        {
            public readonly Assembly assembly;
            public readonly string typeName;

            public TypeKey(Assembly assembly, string typeName)
            {
                this.assembly = assembly;
                this.typeName = typeName;
            }
        }

        private readonly struct MethodKey
        {
            public readonly Type type;
            public readonly string methodName;
            public readonly BindingFlags flags;
            public readonly Binder binder;
            public readonly CallingConventions callConventions;
            public readonly ParamsArray<Type> types;
            public readonly ParameterModifier[] modifiers;

            public MethodKey(
                Type type,
                string methodName,
                BindingFlags flags,
                Binder binder = null,
                CallingConventions callConventions = default,
                ParamsArray<Type> types = default,
                ParameterModifier[] modifiers = null)
            {
                this.type = type;
                this.methodName = methodName;
                this.flags = flags;
                this.binder = binder;
                this.callConventions = callConventions;
                this.types = types;
                this.modifiers = modifiers ?? Array.Empty<ParameterModifier>();
            }
        }

        private static readonly Dictionary<TypeKey, Type> _typeLookup = new();

        // TODO: This probably needs a custom equality comparer for the Type[]/ParameterModifier[], otherwise it'll just do a reference comparison and fail.
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

        // TODO: GetField/GetProperty

        internal static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags, ParamsArray<Type> types = default)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

            if (methodName.Length == 0) throw new ArgumentException("Empty string.", nameof(methodName));

            // TODO: Determine if the flags actually needs to be part of the key.
            var key = new MethodKey(type, methodName, flags, types: types);
            if (!_methodLookup.TryGetValue(key, out MethodInfo method))
            {
                var typesArray = types.ToArray();

                method = type.GetMethod(
                    name: methodName,
                    bindingAttr: flags,
                    binder: null,
                    callConvention: default,
                    types: typesArray,
                    modifiers: null);

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
