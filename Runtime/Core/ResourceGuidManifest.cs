using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

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
        // TODO: EditorCode class?
        internal static bool Editor_TryGetResourcePathNoEditorFallback(Guid guid, out string resourcePath)
        {
            return TryGetResourcePathNoFallback(guid, out resourcePath);
        }

        internal void Editor_AddToCache(Guid guid, string resourcePath)
        {
            Pair pair = new() { guid = guid, path = resourcePath };

            m_pairs ??= new Pair[0];
            UnityEditor.ArrayUtility.Add(ref m_pairs, pair);

            // Clear memory cache
            _lookup = null;
        }

        internal void Editor_SetCache(List<(Guid, string)> list)
        {
            int c = list.Count;
            Pair[] pairs = new Pair[c];

            for (int i = 0; i < c; ++i)
            {
                pairs[i] = new() { guid = list[i].Item1, path = list[i].Item2 };
            }

            m_pairs = pairs;

            // Clear memory cache
            _lookup = null;
        }
#endif // UNITY_EDITOR
    }
}
