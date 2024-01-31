using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JakePerry.Unity
{
    /// <summary>
    /// A simple helper class for adding a <see cref="ScriptableObject"/> instance
    /// to the "Project Settings" window.
    /// </summary>
    internal sealed class ScriptableSettingsProvider : SettingsProvider
    {
        private readonly ScriptableObject m_target;

        private Editor m_editor;
        private bool m_keywordsInitialized;

        /// <summary>
        /// Create a new instance for providing settings in the Project Settings window.
        /// </summary>
        /// <param name="target">
        /// Target scriptable object.
        /// </param>
        /// <param name="path">
        /// Settings display path, ie. "Project/MySettings"
        /// </param>
        /// <param name="isUserSettings">
        /// Indicates whether these settings apply to the current editor user
        /// or to the project.
        /// </param>
        internal ScriptableSettingsProvider(
            ScriptableObject target,
            string path,
            bool isUserSettings)
            : base(path, isUserSettings ? SettingsScope.User : SettingsScope.Project)
        {
            UnityHelper.CheckArgument(target, nameof(target));

            m_target = target;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_editor = Editor.CreateEditor(m_target);

            base.OnActivate(searchContext, rootElement);
        }

        public override void OnDeactivate()
        {
            if (m_editor != null)
            {
                UnityEngine.Object.DestroyImmediate(m_editor);
                m_editor = null;
            }

            base.OnDeactivate();
        }

        public override bool HasSearchInterest(string searchContext)
        {
            if (!m_keywordsInitialized)
            {
                var sObj = new SerializedObject(m_target);
                keywords = GetSearchKeywordsFromSerializedObject(sObj);

                m_keywordsInitialized = true;
            }

            return base.HasSearchInterest(searchContext);
        }

        public override void OnGUI(string searchContext)
        {
            if (m_target == null) return;

            var labelWidth = EditorGUIUtility.labelWidth;

            // Match other settings windows
            EditorGUIUtility.labelWidth = 250;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(10);

                    m_editor.OnInspectorGUI();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}
