using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class ResourceGuidManifestEditorUtil
    {
        const string kAssetsPath = Project.kGeneratedAssetsDir + ResourceGuidManifest.kResourcesPath + ".asset";

        [MenuItem(Project.kContextMenuItemsPath + "Generate/Resources GUID Cache")]
        public static void GenerateResourceGuidManifest()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<ResourceGuidManifest>(kAssetsPath);

            if (manifest == null)
            {
                var manifestPathOnDisk = Path.Combine(Project.GetProjectPath(), kAssetsPath);
                Directory.CreateDirectory(manifestPathOnDisk);

                manifest = ScriptableObject.CreateInstance<ResourceGuidManifest>();
                AssetDatabase.CreateAsset(manifest, kAssetsPath);
            }

            var pairs = new List<(SerializeGuid, string)>();

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (UnityEditorHelper.TryGetResourcesPath(path, out string resourcePath))
                {
                    var guid = Guid.ParseExact(AssetDatabase.GUIDFromAssetPath(path).ToString(), "N");
                    pairs.Add(((SerializeGuid)guid, resourcePath));
                }
            }

            manifest.Editor_SetCache(pairs);

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssetIfDirty(manifest);
        }
    }
}
