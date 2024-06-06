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

            public static readonly Color24 kTypeHover = new(255, 235, 4);
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

        [Header("Type Selector")]

        // TODO: Consider moving this to a new config class. Ideally the settings system could support
        // drawing more than one ScriptableObject within the same path window.
        // TODO: Also would like to support 'linking' to a SerializableColorSwatch (weak link via guid)
        // and using that if set so it gets updated, but you can also break the link and just store the swatch colors.
        // Defaults:
        //      new Color32(214, 157, 133, 255),
        //      new Color32(156, 220, 254, 255),
        //      new Color32(216, 160, 223, 255)
        [SerializeField]
        private ColorSwatch m_typeDisplaySwatch;

        [SerializeField]
        private Color24 m_typeHoverColor = Defaults.kTypeHover;

        private static EditorStylingConfig Cfg => GetSettingsAndCache<EditorStylingConfig>();

        internal static Color24 AliasColor => Cfg.m_aliasColor;

        internal static Color24 ClassColor => Cfg.m_classColor;

        internal static Color24 InterfaceColor => Cfg.m_interfaceColor;

        internal static Color24 StructColor => Cfg.m_structColor;

        internal static ColorSwatch TypeDisplaySwatch => Cfg.m_typeDisplaySwatch;

        internal static Color24 TypeHoverColor => Cfg.m_typeHoverColor;

        // TODO: Button to reset defaults
    }
}
