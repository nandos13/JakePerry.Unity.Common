using UnityEngine;

namespace JakePerry.Unity
{
    [RuntimeSettingsPath("Project/JakePerry/Editor Styling", Order = 150)]
    internal sealed class SerializedTypesStyleConfig : RuntimeSettingsBase
    {
        private static class Defaults
        {
            public static readonly Color24 kTypeHover = new(255, 235, 4);
        }

        [Header("Serialized Types")]

        // TODO: Would like to support 'linking' to a SerializableColorSwatch (weak link via guid)
        // and using that if set so it gets updated, but you can also break the link and just store the swatch colors.
        // Defaults:
        //      new Color32(214, 157, 133, 255),
        //      new Color32(156, 220, 254, 255),
        //      new Color32(216, 160, 223, 255)
        [SerializeField]
        private ColorSwatch m_typeDisplaySwatch;

        [SerializeField]
        private Color24 m_typeHoverColor = Defaults.kTypeHover;

        private static SerializedTypesStyleConfig Cfg => GetSettingsAndCache<SerializedTypesStyleConfig>();

        internal static ColorSwatch TypeDisplaySwatch => Cfg.m_typeDisplaySwatch;

        internal static Color24 TypeHoverColor => Cfg.m_typeHoverColor;
    }
}
