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
            if (_lookup is null)
            {
                var inst = GetInstance();
                var dict = new Dictionary<SerializeGuid, string>();

                if (inst.m_pairs != null)
                    foreach (var pair in inst.m_pairs)
                    {
                        dict[pair.guid] = pair.path;
                    }

                _lookup = dict;
            }
        }

        public static bool TryGetResourcePath(SerializeGuid guid, out string resourcePath)
        {
            InitIfRequired();
            return _lookup.TryGetValue(guid, out resourcePath);
        }

#if UNITY_EDITOR
        internal void Editor_SetCache(List<(SerializeGuid, string)> list)
        {
            int c = list.Count;
            var pairs = new Pair[c];

            for (int i = 0; i < c; ++i)
            {
                pairs[i] = new() { guid = list[i].Item1, path = list[i].Item2 };
            }

            m_pairs = pairs;
        }
#endif // UNITY_EDITOR
    }
}
