using UnityEngine;

namespace JakePerry.Unity
{
    public static class ResourcesEx
    {
        public static bool IsResourcesPath(string path)
        {
            const string kResourcesDir = "/Resources/";

            if (string.IsNullOrEmpty(path))
                return false;

            var resourcesIndex = path.LastIndexOf(kResourcesDir);
            return resourcesIndex > -1
                && resourcesIndex < path.Length - kResourcesDir.Length;
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
            const string kResourcesDir = "/Resources/";

            resourcePath = string.Empty;

            if (!string.IsNullOrEmpty(path))
            {
                // Find the Resources directory in the path
                var resourcesIndex = path.LastIndexOf(kResourcesDir);
                if (resourcesIndex > -1)
                {
                    var start = resourcesIndex + kResourcesDir.Length;
                    var length = path.Length - start;

                    // Remove file extension suffix (eg .prefab, .unity, etc)
                    var lastSeparatorIndex = path.LastIndexOf('/');
                    var lastPeriodIndex = path.LastIndexOf('.');

                    if (lastPeriodIndex > lastSeparatorIndex)
                        length -= (path.Length - lastPeriodIndex);

                    resourcePath = path.Substring(start, length);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Load an asset of the given type with the given guid via the <see cref="Resources"/> API.
        /// </summary>
        /// <typeparam name="T">Resource type.</typeparam>
        /// <param name="guid">Guid of the resource asset.</param>
        public static T Load<T>(SerializeGuid guid)
            where T : UnityEngine.Object
        {
            if (guid.IsDefault) return null;

            if (ResourceGuidManifest.TryGetResourcePath(guid, out string resourcePath))
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
