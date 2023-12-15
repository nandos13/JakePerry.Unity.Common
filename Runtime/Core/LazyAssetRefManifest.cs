using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif // UNITY_EDITOR

namespace JakePerry.Unity
{
    internal sealed class LazyAssetRefManifest : ScriptableObject
#if UNITY_EDITOR
        , IPreprocessBuildWithReport, IPostprocessBuildWithReport, IProcessSceneWithReport
#endif // UNITY_EDITOR
    {
#pragma warning disable CA2235 // Mark all non-serializable fields
        [Serializable]
        private struct Pair { public string guid; public string path; }
#pragma warning restore CA2235

        [SerializeField]
        private Pair[] m_pairs;

        private Dictionary<string, string> m_lookup;

        private Dictionary<string, string> GetLookupDict()
        {
            var dict = new Dictionary<string, string>();

            if (m_pairs != null)
                foreach (var pair in m_pairs)
                    dict[pair.guid] = pair.path;

            return dict;
        }

        public bool TryGetResourcePath(string assetGuid, out string resourcePath)
        {
            if (m_lookup is null)
                m_lookup = GetLookupDict();

            return m_lookup.TryGetValue(assetGuid, out resourcePath);
        }

        public static LazyAssetRefManifest GetInstance()
        {
            // TODO: Better path?
            return Resources.Load<LazyAssetRefManifest>("Internal/LazyAssetRefManifest");
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
            var lookupInst = ScriptableObject.CreateInstance<LazyAssetRefManifest>();

            // TODO: Check all prefabs & scenes in the project.
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

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var components = new List<Component>();
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                components.Clear();
                rootObj.GetComponentsInChildren<Component>(true, components);
                foreach (var c in components)
                {

                }
            }
        }
#endif // UNITY_EDITOR
    }
}
