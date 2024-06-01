using UnityEngine;

namespace JakePerry.Unity
{
    [RuntimeSettingsPath("Project/JakePerry/Editor Styling")]
    internal sealed class EditorStylingConfig : RuntimeSettingsBase
    {
        private static class Defaults
        {
            public static readonly Color24 kAlias = new(86, 156, 214);
            public static readonly Color24 kClass = new(78, 201, 176);
            public static readonly Color24 kInterface = new(184, 215, 163);
            public static readonly Color24 kStruct = new(134, 198, 145);
        }

        [Header("Code Style")]

        [SerializeField]
        private Color24 m_aliasColor = Defaults.kAlias;

        [SerializeField]
        private Color24 m_classColor = Defaults.kClass;

        [SerializeField]
        private Color24 m_interfaceColor = Defaults.kInterface;

        [SerializeField]
        private Color24 m_structColor = Defaults.kStruct;

        private static EditorStylingConfig Cfg => GetSettingsAndCache<EditorStylingConfig>();

        internal static Color24 AliasColor => Cfg.m_aliasColor;

        internal static Color24 ClassColor => Cfg.m_classColor;

        internal static Color24 InterfaceColor => Cfg.m_interfaceColor;

        internal static Color24 StructColor => Cfg.m_structColor;

        // TODO: Button to reset defaults
    }
}
