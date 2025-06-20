#if UNITY_EDITOR

using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Editor-only class defining some meta values for the project.
    /// </summary>
    public static class Project
    {
        public const string ContextMenuItemsPath = "Plugins/JakePerry/";
        public const string GeneratedAssetsDir = "Assets/Generated/JakePerry/";

        /// <summary>
        /// Get the path on disk to the project. This is equal to <see cref="Application.dataPath"/>
        /// without the final "/Assets" directory.
        /// </summary>
        public static string GetProjectPath()
        {
            // Trim "/Assets" from the end of the path
            var path = Application.dataPath;
            return path[..^7];
        }
    }
}

#endif
