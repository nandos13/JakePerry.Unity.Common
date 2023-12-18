using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Defines some meta values for the project.
    /// </summary>
    public static class Project
    {
        public const string kContextMenuItemsPath = "Plugins/JakePerry/";
        public const string kGeneratedAssetsDir = "Assets/Generated/JakePerry/";

        /// <summary>
        /// Get the path on disk to the project. This is equal to
        /// <see cref="Application.dataPath"/> without the
        /// final "/Assets" directory .
        /// </summary>
        public static string GetProjectPath()
        {
            // Trim "/Assets" from the end of the path
            var path = Application.dataPath;
            return path.Substring(0, path.Length - 7);
        }
    }
}
