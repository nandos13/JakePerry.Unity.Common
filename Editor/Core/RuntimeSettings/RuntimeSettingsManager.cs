using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

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
            const BindingFlags kFlags = BindingFlags.Static | BindingFlags.NonPublic;

            var loadMethod = ReflectionEx.GetMethod(typeof(RuntimeSettingsBase), "Load", kFlags, new ParamsArray<Type>(typeof(bool)));
            var genericTypeArgs = new Type[1];

            var invokeArgs = new object[1] { (object)false };

            var list = new List<SettingsProvider>();

            foreach (var t in TypeCache.GetTypesDerivedFrom(typeof(RuntimeSettingsBase)))
            {
                genericTypeArgs[0] = t;
                var method = loadMethod.MakeGenericMethod(genericTypeArgs);

                var settings = (RuntimeSettingsBase)method.Invoke(null, invokeArgs);
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

                    list.Add(new ScriptableSettingsProvider(settings, path, false));
                }
            }

            return list.ToArray();
        }
    }
}
