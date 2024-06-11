using JakePerry.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Responsible for ensuring an asset is created for each type derived from
    /// <see cref="RuntimeSettingsBase"/> &amp; providing a <see cref="SettingsProvider"/>
    /// for each asset.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
    internal static class RuntimeSettingsManager
    {
        private struct Metadata : IComparable<Metadata>
        {
            public RuntimeSettingsBase settings;
            public int order;

            public int CompareTo(Metadata other) => order.CompareTo(other.order);
        }

        [InitializeOnLoadMethod]
        [MenuItem(Project.kContextMenuItemsPath + "Settings/Create missing settings assets")]
        private static void CreateMissingSettingsAssets()
        {
            bool didCreateAnyAssets = false;

            foreach (var t in TypeCache.GetTypesDerivedFrom(typeof(RuntimeSettingsBase)))
            {
                RuntimeSettingsBase.Load(createIfMissing: true, type: t, out bool created);
                didCreateAnyAssets |= created;
            }

            if (didCreateAnyAssets)
            {
#pragma warning disable UNT0031 // Asset operations in LoadAttribute method
                // Justification: This will only run when a new settings type
                // is added to the project.
                AssetDatabase.SaveAssets();
#pragma warning restore UNT0031
            }

            SettingsService.NotifySettingsProviderChanged();
        }

        [SettingsProviderGroup]
        private static SettingsProvider[] CreateSettingsProviders()
        {
            var dict = new ContiguousDictionary<string, List<Metadata>>(Comparers<string>.Create(StringComparer.Ordinal));

            foreach (var t in TypeCache.GetTypesDerivedFrom(typeof(RuntimeSettingsBase)))
            {
                var settings = RuntimeSettingsBase.Load(false, t, out _);
                if (settings != null)
                {
                    string path;

                    var pathAttr = t.GetCustomAttribute<RuntimeSettingsPathAttribute>();
                    if (pathAttr != null)
                    {
                        path = pathAttr.Path;
                    }
                    else
                    {
                        var niceName = ObjectNames.NicifyVariableName(t.Name);
                        path = $"Project/{niceName}";
                    }

                    if (!dict.TryGetValue(path, out var metadata))
                    {
                        dict[path] = metadata = new List<Metadata>(capacity: 4);
                    }

                    metadata.Add(new Metadata { settings = settings, order = pathAttr?.Order ?? 0 });
                }
            }

            var list = new List<SettingsProvider>();
            var list2 = new List<ScriptableObject>();

            foreach (var pair in dict)
            {
                var path = pair.Key;
                var metadata = pair.Value;
                metadata.Sort();

                list2.Clear();
                foreach (var m in metadata)
                {
                    list2.Add(m.settings);
                }

                list.Add(new ScriptableSettingsProvider(list2, path, false));
            }

            return list.ToArray();
        }
    }
}
