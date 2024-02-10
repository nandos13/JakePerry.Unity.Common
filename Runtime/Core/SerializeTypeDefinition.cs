using System;
using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    // TODO: Documentation pass
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

        public bool IsNull => string.IsNullOrEmpty(m_typeName);

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        [UnityEditor.InitializeOnLoadMethod]
        private static void ClearCache()
        {
            _cache.Clear();
        }
#endif

        internal Type ResolveTypeUnbound(bool throwOnError)
        {
            if (IsNull)
            {
                if (throwOnError) throw new InvalidOperationException("Type name unassigned.");
                return null;
            }

            if (!_cache.TryGetValue(m_typeName, out Type t))
            {
                t = Type.GetType(m_typeName, throwOnError: throwOnError, ignoreCase: false);
                _cache[m_typeName] = t;
            }

            return t;
        }

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

        public Type ResolveType() => ResolveType(false);
    }
}
