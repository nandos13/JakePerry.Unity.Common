using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// Caches metadata for a given <see cref="UnityReturnDelegateBase"/> type.
    /// </summary>
    internal sealed class DelegateMetadata
    {
        private static readonly Dictionary<Type, DelegateMetadata> _metadataCache = new();

        internal readonly Type delegateType;
        internal readonly Type returnType;
        internal readonly Type[] eventDefinedArgs;

        internal DelegateMetadata(UnityReturnDelegateBase dummy)
        {
            delegateType = dummy.GetType();
            returnType = dummy.ReturnType;
            eventDefinedArgs = dummy.GetEventDefinedInvocationArgumentTypes();
        }

        internal static DelegateMetadata GetMetadata(Type type)
        {
            if (!_metadataCache.TryGetValue(type, out var metadata))
            {
                var dummy = (UnityReturnDelegateBase)Activator.CreateInstance(type);
                metadata = new DelegateMetadata(dummy);
                _metadataCache[type] = metadata;
            }
            return metadata;
        }

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void OnRecompile()
        {
            _metadataCache.Clear();
        }
    }
}
