using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace JakePerry.Unity
{
    internal sealed class ResourceGuidManifest : ScriptableObject
#if UNITY_EDITOR
        , IPreprocessBuildWithReport, IPostprocessBuildWithReport, IProcessSceneWithReport
#endif // UNITY_EDITOR
    {
        [Serializable]
        private struct Pair { public string guid; public string path; }

        private static Dictionary<string, string> _lookup;

        [SerializeField]
        private Pair[] m_pairs;

        private static ResourceGuidManifest GetInstance()
        {
            return Resources.Load<ResourceGuidManifest>("Internal/ResourceGuidManifest");
        }

        private static void InitIfRequired()
        {
            if (_lookup is null)
            {
                var inst = GetInstance();
                var dict = new Dictionary<string, string>(StringComparer.Ordinal);

                if (inst.m_pairs != null)
                    foreach (var pair in inst.m_pairs)
                    {
                        dict[pair.guid] = pair.path;
                    }

                _lookup = dict;
            }
        }

        public static bool TryGetResourcePath(string guid, out string resourcePath)
        {
            InitIfRequired();
            return _lookup.TryGetValue(guid, out resourcePath);
        }

#if UNITY_EDITOR
        private static Dictionary<Type, MemberInfo[]> _membersByType;

        private static void GetNonPublicFields(Type t, List<FieldInfo> results)
        {
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (field.DeclaringType != t) continue;

                // TODO: Find all serialized fields, find fields of type LazyAssetRef
            }
        }

        int IOrderedCallback.callbackOrder => 1000;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            var lookupInst = ScriptableObject.CreateInstance<ResourceGuidManifest>();

            // TODO: Check all prefabs & scenes in the project.
            // TODO: Have ability to ONLY grab resources that also have a given label,
            // for project with shit loads of resources but maybe most dont use this system.
            var referencedGuids = new DistinctList<string>();

            var pairs = new List<Pair>();
            foreach (var guid in referencedGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    pairs.Add(new Pair() { guid = guid, path = path });
                }
            }

            lookupInst.m_pairs = pairs.ToArray();
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            // TODO: Find all serialized lazy refs in all assets that made it into the build...
            foreach (var assets in report.packedAssets)
            {
                foreach (var asset in assets.contents)
                {
                    var path = asset.sourceAssetPath;
                }
            }
        }

        void IProcessSceneWithReport.OnProcessScene(Scene scene, BuildReport report)
        {
            var components = new List<Component>();
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                components.Clear();
                rootObj.GetComponentsInChildren<Component>(true, components);
                foreach (var c in components)
                {
                    // TODO?
                }
            }
        }
#endif // UNITY_EDITOR
    }
}
