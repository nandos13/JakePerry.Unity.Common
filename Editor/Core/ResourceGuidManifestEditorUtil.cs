using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class ResourceGuidManifestEditorUtil
    {
        const string kAssetsPath = Project.kGeneratedAssetsDir + "Resources/" + ResourceGuidManifest.ResourcesPath + ".asset";

        internal static ResourceGuidManifest GetOrCreateManifestAsset()
        {
            ResourceGuidManifest manifest = AssetDatabase.LoadAssetAtPath<ResourceGuidManifest>(kAssetsPath);

            if (manifest == null)
            {
                string manifestPathOnDisk = Path.Combine(Project.GetProjectPath(), kAssetsPath);
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
            List<(Guid, string)> pairs = new();

            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (ResourcesEx.TryGetResourcesPath(path, out string resourcePath))
                {
                    Guid guid = Guid.ParseExact(AssetDatabase.GUIDFromAssetPath(path).ToString(), "N");
                    pairs.Add((guid, resourcePath));
                }
            }

            ResourceGuidManifest manifest = GetOrCreateManifestAsset();
            manifest.Editor_SetCache(pairs);

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssetIfDirty(manifest);
        }
    }
}
