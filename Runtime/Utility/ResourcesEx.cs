using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class ResourcesEx
    {
        /// <summary>
        /// Load an asset of the given type with the given guid via the <see cref="Resources"/> API.
        /// </summary>
        /// <typeparam name="T">Resource type.</typeparam>
        /// <param name="guid">Guid of the resource asset.</param>
        public static T Load<T>(SerializeGuid guid)
            where T : UnityEngine.Object
        {
            if (guid.IsDefault) return null;

            if (ResourceGuidManifest.TryGetResourcePath(guid.UnityGuidString, out string resourcePath))
            {
                return Resources.Load<T>(resourcePath);
            }

            return null;
        }

        /// <summary>
        /// Load an asset with the given guid via the <see cref="Resources"/> API.
        /// </summary>
        /// <inheritdoc cref="Load{T}(SerializeGuid)"/>
        public static UnityEngine.Object Load(SerializeGuid guid) => Load<UnityEngine.Object>(guid);

        /// <summary>
        /// Attempt to load an asset of the given type with the given guid via the <see cref="Resources"/> API.
        /// </summary>
        /// <inheritdoc cref="Load{T}(SerializeGuid)"/>
        /// <param name="resource">The loaded resource, if one was loaded.</param>
        /// <returns>
        /// <see langword="true"/> if a resource was successfully loaded; Otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryLoad<T>(SerializeGuid guid, out T resource)
            where T : UnityEngine.Object
        {
            resource = Load<T>(guid);
            return resource != null;
        }

        /// <summary>
        /// Attempt to load an asset with the given guid via the <see cref="Resources"/> API.
        /// </summary>
        /// <inheritdoc cref="TryLoad{T}(SerializeGuid, out T)"/>
        public static bool TryLoad(SerializeGuid guid, out UnityEngine.Object resource)
        {
            return TryLoad<UnityEngine.Object>(guid, out resource);
        }
    }
}
