using JakePerry.Unity.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// A serializable data structure that encapsulates a <see cref="Type"/>,
    /// with support for both bound and unbound generics.
    /// </summary>
    [Serializable]
    public partial struct SerializeTypeDefinition
    {
        private static readonly Dictionary<string, Type> _cache = new(StringComparer.Ordinal);

        [SerializeField]
        private string m_typeName;

        [SerializeField]
        private bool m_wantsUnboundGeneric;

        [SerializeField]
        private SerializeTypeDefinition[] m_genericArgs;

        /// <summary>
        /// Indicates whether the type was set to <see langword="null"/>
        /// at serialization time.
        /// </summary>
        public bool IsNull => string.IsNullOrEmpty(m_typeName);

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        [UnityEditor.InitializeOnLoadMethod]
        private static void ClearCache()
        {
            _cache.Clear();
        }
#endif

        internal static Type ResolveTypeWithCache(string typeName, bool throwOnError, bool cacheFailure)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                if (throwOnError) throw new InvalidOperationException("Type name unassigned.");
                return null;
            }

            if (!_cache.TryGetValue(typeName, out Type t))
            {
                t = Type.GetType(typeName, throwOnError: throwOnError, ignoreCase: false);

                if (t is not null || cacheFailure)
                {
                    _cache[typeName] = t;
                }
            }

            return t;
        }

        internal Type ResolveTypeUnbound(bool throwOnError)
        {
            return ResolveTypeWithCache(m_typeName, throwOnError, true);
        }

        /// <summary>
        /// Resolve the <see cref="Type"/> that was serialized.
        /// </summary>
        /// <param name="throwOnError">
        /// If <see langword="true"/>, an exception is thrown if the type cannot be resolved.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/> that was serialized, or <see langword="null"/> if no type
        /// could be resolved and <paramref name="throwOnError"/> is <see langword="false"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException"/>
        public Type ResolveType(bool throwOnError)
        {
            var t = ResolveTypeUnbound(throwOnError);

            if (t is not null &&
                t.IsGenericTypeDefinition &&
                !m_wantsUnboundGeneric &&
                (m_genericArgs?.Length ?? 0) > 0)
            {
                var typeName = t.Name;

                var intPart = typeName.AsSpan(typeName.LastIndexOf('`') + 1);
                var argumentCount = int.Parse(intPart);

                if (m_genericArgs.Length != argumentCount)
                {
                    var message = "Incorrect number of generic arguments were serialized.";
                    if (throwOnError) throw new InvalidOperationException(message);
                    Debug.LogError(message);
                    return null;
                }

                var genericArguments = new Type[argumentCount];
                for (int i = 0; i < argumentCount; ++i)
                {
                    var arg = m_genericArgs[i].ResolveType();

                    if (arg is null)
                    {
                        if (throwOnError)
                        {
                            var message = "A generic type is assigned but one or more generic arguments were unable to be resolved.";
                            throw new InvalidOperationException(message);
                        }
                        return null;
                    }

                    genericArguments[i] = arg;
                }

                t = t.MakeGenericType(genericArguments);
            }

            return t;
        }

        /// <returns>
        /// The <see cref="Type"/> that was serialized, or <see langword="null"/> if no type
        /// could be resolved.
        /// </returns>
        /// <inheritdoc cref="ResolveType(bool)"/>
        public Type ResolveType() => ResolveType(false);
    }
}
