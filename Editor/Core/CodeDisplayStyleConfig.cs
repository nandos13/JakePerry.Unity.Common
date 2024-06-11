using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    [RuntimeSettingsPath("Project/JakePerry/Editor Styling", Order = 100)]
    internal sealed class CodeDisplayStyleConfig : RuntimeSettingsBase
    {
        private static class Defaults
        {
            public static readonly Color24 kAlias = new(86, 156, 214);
            public static readonly Color24 kClass = new(78, 201, 176);
            public static readonly Color24 kInterface = new(184, 215, 163);
            public static readonly Color24 kStruct = new(134, 198, 145);
        }

        [Header("Code Display")]

        [SerializeField]
        private Color24 m_aliasColor = Defaults.kAlias;

        [SerializeField]
        private Color24 m_classColor = Defaults.kClass;

        [SerializeField]
        private Color24 m_interfaceColor = Defaults.kInterface;

        [SerializeField]
        private Color24 m_structColor = Defaults.kStruct;

        private static CodeDisplayStyleConfig Cfg => GetSettingsAndCache<CodeDisplayStyleConfig>();

        internal static Color24 AliasColor => Cfg.m_aliasColor;

        internal static Color24 ClassColor => Cfg.m_classColor;

        internal static Color24 InterfaceColor => Cfg.m_interfaceColor;

        internal static Color24 StructColor => Cfg.m_structColor;

        internal void ResetToDefault()
        {
            m_aliasColor = Defaults.kAlias;
            m_classColor = Defaults.kClass;
            m_interfaceColor = Defaults.kInterface;
            m_structColor = Defaults.kStruct;
        }
    }

    [CustomEditor(typeof(CodeDisplayStyleConfig))]
    internal sealed class EditorStylingConfigInspector : Editor
    {
        private static readonly GUILayoutOption[] _btnOptions = new GUILayoutOption[]
        {
            GUILayout.Width(120)
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Reset to Default", _btnOptions))
            {
                var cfg = (CodeDisplayStyleConfig)target;

                Undo.RecordObject(cfg, "Reset to default");
                cfg.ResetToDefault();

                EditorUtility.SetDirty(cfg);
            }
        }
    }
}
