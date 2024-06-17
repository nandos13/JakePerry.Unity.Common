using JakePerry.Collections;
using UnityEngine;

namespace JakePerry.Unity
{
    [RuntimeSettingsPath("Project/JakePerry/Editor Styling", Order = 150)]
    internal sealed class SerializedTypesStyleConfig : RuntimeSettingsBase
    {
        private static class Defaults
        {
            public static readonly Color24[] kTypeDisplay = new Color24[]
            {
                new(214, 157, 133),
                new(156, 220, 254),
                new(216, 160, 223)
            };

            public static readonly Color24 kTypeHover = new(255, 235, 4);
        }

        [Header("Serialized Types")]

        [SerializeField]
        private LinkableData<Color24[]> m_typeDisplaySwatch = Defaults.kTypeDisplay;

        [SerializeField]
        private Color24 m_typeHoverColor = Defaults.kTypeHover;

        private static SerializedTypesStyleConfig Cfg => GetSettingsAndCache<SerializedTypesStyleConfig>();

        internal static ReadOnlyArray<Color24> TypeDisplaySwatch => Cfg.m_typeDisplaySwatch.Value;

        internal static Color24 TypeHoverColor => Cfg.m_typeHoverColor;
    }
}
