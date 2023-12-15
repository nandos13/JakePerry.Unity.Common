using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class LazyAssetRefEditorUtil
    {
        /// <summary>
        /// Attempts to find the Resources path for an asset with the given Guid.
        /// </summary>
        /// <param name="guid">
        /// Guid of the project asset.
        /// </param>
        /// <inheritdoc cref="UnityEditorHelper.TryGetResourcesPath(string, out string)"/>
        public static bool TryGetResourcePath(SerializeGuid guid, out string resourcePath)
        {
            var guidString = guid.UnityGuidString;
            var assetPath = AssetDatabase.GUIDToAssetPath(guidString);

            return UnityEditorHelper.TryGetResourcesPath(assetPath, out resourcePath);
        }
    }
}
