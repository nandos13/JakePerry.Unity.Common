using System;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace JakePerry.Unity.Events
{
    // TODO: Document thoroughly.
    // Implementations MUST provide a 'm_value' field with SerializeField or SerializeReference
    // Maybe I could look at adding a compile-time check for it?
    [Serializable]
    public abstract class SerializableMethodArgument
    {
        private static readonly Dictionary<Type, FieldInfo> _invocationArgValueFieldCache = new();

        /// <summary>
        /// Obtain the <see cref="FieldInfo"/> which contains the argument value.
        /// </summary>
        /// <param name="t">
        /// A type derived from <see cref="SerializableMethodArgument"/>.
        /// </param>
        internal static FieldInfo GetArgumentValueField(Type t)
        {
            const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Enforce.Argument(t, nameof(t)).IsNotNull();
            Enforce.Argument(t, nameof(t)).IsAssignableTo(typeof(SerializableMethodArgument));

            if (!_invocationArgValueFieldCache.TryGetValue(t, out var field))
            {
                field = t.GetField("m_value", kFlags);
                _invocationArgValueFieldCache[t] = field;
            }
            return field;
        }

#if UNITY_EDITOR
        public static class EditorUtil
        {
            /// <summary>
            /// Find a type derived from <see cref="SerializableMethodArgument"/> which is
            /// capable of serializing the given type, if one exists.
            /// </summary>
            /// <param name="t">
            /// The type to be serialized.
            /// </param>
            internal static Type FindContainerForType(Type t)
            {
                foreach (var t2 in TypeCache.GetTypesDerivedFrom(typeof(SerializableMethodArgument)))
                {
                    var field = GetArgumentValueField(t2);
                    if (field is not null && field.FieldType.IsAssignableFrom(t))
                    {
                        return t2;
                    }
                }
                return null;
            }
        }

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void OnRecompile()
        {
            _invocationArgValueFieldCache.Clear();
        }
#endif
    }
}
