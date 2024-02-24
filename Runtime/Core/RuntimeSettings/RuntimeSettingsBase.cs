using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Base class for runtime settings/configuration objects.
    /// Simply derive a class from this type and an instance will be automatically created
    /// and added to the "Project Settings" window.
    /// </summary>
    public abstract class RuntimeSettingsBase : ScriptableObject
    {
        private const string kSettingsDir = Project.kGeneratedAssetsDir + "Resources/Settings/";

        private static readonly Dictionary<Type, RuntimeSettingsBase> _cache = new();

        private static RuntimeSettingsBase Load_Internal(bool createIfMissing, Type type, out bool created)
        {
            created = false;

            var nameAttr = type.GetCustomAttribute<RuntimeSettingsAssetNameAttribute>(true);

            var assetName = nameAttr?.AssetName;
            if (string.IsNullOrWhiteSpace(assetName)) assetName = type.Name;

            var asset = Resources.Load($"Settings/{assetName}", type);

            // If no existing asset is found, we must create a new one.
            if (asset == null)
            {
                if (!createIfMissing) return null;

                asset = CreateInstance(type);
                asset.name = assetName;

#if UNITY_EDITOR
                var settingsPath = kSettingsDir + assetName + ".asset";

                var pathOnDisk = System.IO.Path.Combine(Project.GetProjectPath(), settingsPath);
                new System.IO.FileInfo(pathOnDisk).Directory.Create();

                UnityEditor.AssetDatabase.CreateAsset(asset, settingsPath);
                UnityEditor.EditorUtility.SetDirty(asset);

                Debug.Log($"Created settings asset {assetName}", asset);
#endif

                created = true;
            }

            return (RuntimeSettingsBase)asset;
        }

        private static T Load<T>(bool createIfMissing)
            where T : RuntimeSettingsBase
        {
            return (T)Load_Internal(createIfMissing, typeof(T), out _);
        }

        internal static RuntimeSettingsBase Load(bool createIfMissing, Type type, out bool created)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            if (!typeof(RuntimeSettingsBase).IsAssignableFrom(type))
            {
                throw new ArgumentException(nameof(type));
            }

            return Load_Internal(createIfMissing, type, out created);
        }

        protected static T GetSettingsAndCache<T>(bool createIfMissing = true)
            where T : RuntimeSettingsBase
        {
            if (!_cache.TryGetValue(typeof(T), out var settings))
            {
                settings = Load<T>(createIfMissing);
                _cache[typeof(T)] = settings;
            }

            return (T)settings;
        }
    }
}
