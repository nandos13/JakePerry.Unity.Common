using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JakePerry.Unity
{
    internal sealed class ResourceGuidManifest : ScriptableObject
    {
        public const string ResourcesPath = "Internal/ResourceGuidManifest";

        private static readonly ProfilerMarker _profiling_tryGetResourcePath = new(nameof(TryGetResourcePath));

        [Serializable]
        private struct Pair { public PackedGuid guid; public string path; }

        private static Dictionary<PackedGuid, string> _lookup;

        [SerializeField]
        private Pair[] m_pairs;

        private static ResourceGuidManifest GetInstance()
        {
            return Resources.Load<ResourceGuidManifest>(ResourcesPath);
        }

        private static void InitIfRequired()
        {
#if UNITY_EDITOR

            if (GetInstance() == null) _lookup = null;

#endif // UNITY_EDITOR

            if (_lookup is null)
            {
                ResourceGuidManifest inst = GetInstance();
                Dictionary<PackedGuid, string> dict = new();

                if (inst != null && inst.m_pairs is not null)
                    foreach (Pair pair in inst.m_pairs)
                    {
                        dict[pair.guid] = pair.path;
                    }

                _lookup = dict;
            }
        }

        private static bool TryGetResourcePathNoFallback(PackedGuid guid, out string resourcePath)
        {
            InitIfRequired();
            return _lookup.TryGetValue(guid, out resourcePath);
        }

#if UNITY_EDITOR
        private static bool Editor_TryGetTrueResourcePath(PackedGuid guid, out string resourcePath)
        {
            if (!guid.IsDefaultValue)
            {
                string assetPath = UnityEditorHelper.GetProjectAssetPath(guid);
                return ResourcesEx.TryGetResourcesPath(assetPath, out resourcePath);
            }

            return TryGet.Fail(out resourcePath);
        }
#endif // UNITY_EDITOR

        public static bool TryGetResourcePath(Guid guid, out string resourcePath)
        {
            using var profilingScope = _profiling_tryGetResourcePath.Auto();

            if (TryGetResourcePathNoFallback(guid, out resourcePath))
            {
#if UNITY_EDITOR
                using var profilingScope2 = ProfilingEx.Markers.EditorOnly.Auto();

                // Editor validation: Check that the cached resource path is accurate
                if (Editor_TryGetTrueResourcePath(guid, out string editorResourcePath) &&
                    !StringComparer.Ordinal.Equals(resourcePath, editorResourcePath))
                {
                    string unityGuidString = UnityHelper.GetUnityGuidString(guid);
                    Debug.LogError(
                        $"Resources manifest contains incorrect path for guid {unityGuidString}. This will cause a failure in build.\n" +
                        $"Current path: {resourcePath}" +
                        $"Expected path: {editorResourcePath}\n");

                    return true;
                }
#endif // UNITY_EDITOR

                return true;
            }

#if UNITY_EDITOR
            using var profilingScope3 = ProfilingEx.Markers.EditorOnly.Auto();

            // Editor fallback: Gracefully load resources that are not in the manifest & log an error.
            if (Editor_TryGetTrueResourcePath(guid, out resourcePath))
            {
                string unityGuidString = UnityHelper.GetUnityGuidString(guid);
                Debug.LogError(
                    $"Resources manifest does not contain path for guid {unityGuidString}. This will cause a failure in build.\n" +
                    $"Expected path: {resourcePath}");

                return true;
            }
#endif // UNITY_EDITOR

            return TryGet.Fail(out resourcePath);
        }

#if UNITY_EDITOR
        internal static class EditorCode
        {
            const string AssetsPath = Project.GeneratedAssetsDir + "Resources/" + ResourcesPath + ".asset";

            internal static bool TryGetResourcePathNoEditorFallback(Guid guid, out string resourcePath)
            {
                return TryGetResourcePathNoFallback(guid, out resourcePath);
            }

            internal static void AddToCache(ResourceGuidManifest manifest, Guid guid, string resourcePath)
            {
                Enforce.Argument(manifest, nameof(manifest)).IsNotNull();

                Pair pair = new() { guid = guid, path = resourcePath };

                manifest.m_pairs ??= Array.Empty<Pair>();

                UnityEditor.ArrayUtility.Add(ref manifest.m_pairs, pair);

                // Clear memory cache
                _lookup = null;
            }

            internal static void SetCache(ResourceGuidManifest manifest, List<(Guid, string)> list)
            {
                Enforce.Argument(manifest, nameof(manifest)).IsNotNull();
                Enforce.Argument(list, nameof(list)).IsNotNull();

                int c = list.Count;
                Pair[] pairs = new Pair[c];

                for (int i = 0; i < c; ++i)
                {
                    pairs[i] = new Pair() { guid = list[i].Item1, path = list[i].Item2 };
                }

                manifest.m_pairs = pairs;

                // Clear memory cache
                _lookup = null;
            }

            internal static ResourceGuidManifest GetOrCreateManifestAsset()
            {
                ResourceGuidManifest manifest = AssetDatabase.LoadAssetAtPath<ResourceGuidManifest>(AssetsPath);

                if (manifest == null)
                {
                    string manifestPathOnDisk = Path.Combine(Project.GetProjectPath(), AssetsPath);
                    new FileInfo(manifestPathOnDisk).Directory.Create();

                    manifest = ScriptableObject.CreateInstance<ResourceGuidManifest>();
                    AssetDatabase.CreateAsset(manifest, AssetsPath);

                    EditorUtility.SetDirty(manifest);
                }

                return manifest;
            }

            [MenuItem(Project.ContextMenuItemsPath + "Generate/Resources GUID Cache")]
            internal static void GenerateResourceGuidManifest()
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

                ResourceGuidManifest.EditorCode.SetCache(manifest, pairs);

                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssetIfDirty(manifest);
            }
        }
#endif // UNITY_EDITOR
    }
}
