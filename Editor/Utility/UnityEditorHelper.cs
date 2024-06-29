using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class UnityEditorHelper
    {
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

        /// <summary>
        /// Attempts to find the Resources-relative path for an asset with the given guid.
        /// </summary>
        /// <param name="guid">Guid of the resource asset.</param>
        /// <inheritdoc cref="ResourcesEx.TryGetResourcesPath(string, out string)"/>
        public static bool TryGetResourcesPathFromAssetGuid(SerializeGuid guid, out string resourcePath)
        {
            resourcePath = string.Empty;
            return TryGetAssetPath(guid, out string assetPath)
                && ResourcesEx.TryGetResourcesPath(assetPath, out resourcePath);
        }
    }
}
