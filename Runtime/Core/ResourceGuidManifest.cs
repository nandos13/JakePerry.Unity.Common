using System;
using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    internal sealed class ResourceGuidManifest : ScriptableObject
    {
        public const string kResourcesPath = "Internal/ResourceGuidManifest";

        [Serializable]
        private struct Pair { public SerializeGuid guid; public string path; }

        private static Dictionary<SerializeGuid, string> _lookup;

        [SerializeField]
        private Pair[] m_pairs;

        private static ResourceGuidManifest GetInstance()
        {
            return Resources.Load<ResourceGuidManifest>(kResourcesPath);
        }

        private static void InitIfRequired()
        {
#if UNITY_EDITOR

            if (GetInstance() == null) _lookup = null;

#endif // UNITY_EDITOR

            if (_lookup is null)
            {
                var inst = GetInstance();
                var dict = new Dictionary<SerializeGuid, string>();

                if (inst != null && inst.m_pairs != null)
                    foreach (var pair in inst.m_pairs)
                    {
                        dict[pair.guid] = pair.path;
                    }

                _lookup = dict;
            }
        }

        private static bool TryGetResourcePathNoFallback(SerializeGuid guid, out string resourcePath)
        {
            InitIfRequired();
            return _lookup.TryGetValue(guid, out resourcePath);
        }

#if UNITY_EDITOR

        private static bool Editor_TryGetTrueResourcePath(SerializeGuid guid, out string resourcePath)
        {
            if (!guid.IsDefault)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid.UnityGuidString);
                return ResourcesEx.TryGetResourcesPath(assetPath, out resourcePath);
            }

            resourcePath = null;
            return false;
        }

#endif // UNITY_EDITOR

        public static bool TryGetResourcePath(SerializeGuid guid, out string resourcePath)
        {
            if (TryGetResourcePathNoFallback(guid, out resourcePath))
            {
#if UNITY_EDITOR

                // Editor validation: Check that the cached resource path is accurate
                UnityEngine.Profiling.Profiler.BeginSample("[Editor Only] TryGetResourcePath");
                if (Editor_TryGetTrueResourcePath(guid, out string editorResourcePath) &&
                    !StringComparer.Ordinal.Equals(resourcePath, editorResourcePath))
                {
                    Debug.LogError(
                        $"Resources manifest contains incorrect path for guid {guid.UnityGuidString}. This will cause a failure in build.\n" +
                        $"Current path: {resourcePath}" +
                        $"Expected path: {editorResourcePath}\n");

                    UnityEngine.Profiling.Profiler.EndSample();
                    return true;
                }
                UnityEngine.Profiling.Profiler.EndSample();

#endif // UNITY_EDITOR

                return true;
            }

#if UNITY_EDITOR

            UnityEngine.Profiling.Profiler.BeginSample("[Editor Only] TryGetResourcePath");
            // Editor fallback: Gracefully load resources that are not in the manifest & log an error.
            if (Editor_TryGetTrueResourcePath(guid, out resourcePath))
            {
                Debug.LogError(
                    $"Resources manifest does not contain path for guid {guid.UnityGuidString}. This will cause a failure in build.\n" +
                    $"Expected path: {resourcePath}");

                UnityEngine.Profiling.Profiler.EndSample();
                return true;
            }
            UnityEngine.Profiling.Profiler.EndSample();

#endif // UNITY_EDITOR

            resourcePath = null;
            return false;
        }

#if UNITY_EDITOR
        internal static bool Editor_TryGetResourcePathNoEditorFallback(SerializeGuid guid, out string resourcePath)
        {
            return TryGetResourcePathNoFallback(guid, out resourcePath);
        }

        internal void Editor_AddToCache(SerializeGuid guid, string resourcePath)
        {
            var pair = new Pair() { guid = guid, path = resourcePath };

            m_pairs ??= new Pair[0];
            UnityEditor.ArrayUtility.Add(ref m_pairs, pair);

            // Clear memory cache
            _lookup = null;
        }

        internal void Editor_SetCache(List<(SerializeGuid, string)> list)
        {
            int c = list.Count;
            var pairs = new Pair[c];

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
