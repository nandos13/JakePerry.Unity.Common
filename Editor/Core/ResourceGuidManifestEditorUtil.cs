using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class ResourceGuidManifestEditorUtil
    {
        const string kAssetsPath = Project.kGeneratedAssetsDir + "Resources/" + ResourceGuidManifest.kResourcesPath + ".asset";

        internal static ResourceGuidManifest GetOrCreateManifestAsset()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<ResourceGuidManifest>(kAssetsPath);

            if (manifest == null)
            {
                var manifestPathOnDisk = Path.Combine(Project.GetProjectPath(), kAssetsPath);
                new FileInfo(manifestPathOnDisk).Directory.Create();

                manifest = ScriptableObject.CreateInstance<ResourceGuidManifest>();
                AssetDatabase.CreateAsset(manifest, kAssetsPath);

                EditorUtility.SetDirty(manifest);
            }

            return manifest;
        }

        [MenuItem(Project.kContextMenuItemsPath + "Generate/Resources GUID Cache")]
        public static void GenerateResourceGuidManifest()
        {
            var pairs = new List<(SerializeGuid, string)>();

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (UnityEditorHelper.TryGetResourcesPath(path, out string resourcePath))
                {
                    var guid = Guid.ParseExact(AssetDatabase.GUIDFromAssetPath(path).ToString(), "N");
                    pairs.Add(((SerializeGuid)guid, resourcePath));
                }
            }

            var manifest = GetOrCreateManifestAsset();
            manifest.Editor_SetCache(pairs);

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssetIfDirty(manifest);
        }
    }
}
