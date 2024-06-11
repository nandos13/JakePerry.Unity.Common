using JakePerry.Collections;
using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// A simple helper class for adding one or more <see cref="ScriptableObject"/> instances
    /// to the "Project Settings" window.
    /// </summary>
    internal sealed class ScriptableSettingsProvider : SettingsProvider
    {
        private readonly ScriptableObject[] m_targets;
        private readonly Editor[] m_editors;

        private bool m_keywordsInitialized;

        /// <summary>
        /// Create a new instance for providing settings in the Project Settings window.
        /// </summary>
        /// /// <param name="path">
        /// Settings display path, ie. "Project/MySettings"
        /// </param>
        /// <param name="isUserSettings">
        /// Indicates whether these settings apply to the current editor user
        /// or to the project.
        /// </param>
        private ScriptableSettingsProvider(string path, bool isUserSettings)
            : base(path, isUserSettings ? SettingsScope.User : SettingsScope.Project)
        { }

        /// <param name="target">
        /// Target scriptable object.
        /// </param>
        /// <inheritdoc cref="ScriptableSettingsProvider(string, bool)"/>
        internal ScriptableSettingsProvider(
            ScriptableObject target,
            string path,
            bool isUserSettings)
            : this(path, isUserSettings)
        {
            UnityHelper.CheckArgument(target, nameof(target));

            m_targets = new ScriptableObject[1] { target };
            m_editors = new Editor[1];
        }

        /// <param name="targets">
        /// Target scriptable objects.
        /// </param>
        /// <inheritdoc cref="ScriptableSettingsProvider(string, bool)"/>
        internal ScriptableSettingsProvider(
            ReadOnlyArray<ScriptableObject> targets,
            string path,
            bool isUserSettings)
            : this(path, isUserSettings)
        {
            if (targets == default) throw new ArgumentNullException(nameof(targets));

            m_targets = targets.Copy();
            m_editors = new Editor[m_targets.Length];
        }

        /// <param name="targets">
        /// Target scriptable objects.
        /// </param>
        /// <inheritdoc cref="ScriptableSettingsProvider(string, bool)"/>
        internal ScriptableSettingsProvider(
            ReadOnlyList<ScriptableObject> targets,
            string path,
            bool isUserSettings)
            : this(path, isUserSettings)
        {
            if (targets == default) throw new ArgumentNullException(nameof(targets));

            m_targets = targets.ToArray();
            m_editors = new Editor[m_targets.Length];
        }

        public override void OnDeactivate()
        {
            for (int i = 0; i < m_editors.Length; ++i)
            {
                var editor = m_editors[i];
                if (editor != null)
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                    m_editors[i] = null;
                }
            }

            base.OnDeactivate();
        }

        public override bool HasSearchInterest(string searchContext)
        {
            if (!m_keywordsInitialized)
            {
                if (m_targets.Length == 1)
                {
                    var sObj = new SerializedObject(m_targets[0]);
                    keywords = GetSearchKeywordsFromSerializedObject(sObj);
                }
                else
                {
                    var list = new DistinctList<string>(StringComparer.Ordinal);
                    foreach (var target in m_targets)
                    {
                        var sObj = new SerializedObject(target);
                        list.AddRange(GetSearchKeywordsFromSerializedObject(sObj));
                    }

                    keywords = list;
                }

                m_keywordsInitialized = true;
            }

            return base.HasSearchInterest(searchContext);
        }

        public override void OnGUI(string searchContext)
        {
            var labelWidth = EditorGUIUtility.labelWidth;

            // Match other settings windows
            EditorGUIUtility.labelWidth = 250;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);

                GUILayout.BeginVertical();
                {
                    int count = m_targets.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        var target = m_targets[i];
                        if (target == null) continue;

                        GUILayout.Space(14);

                        var editor = m_editors[i];
                        if (editor == null)
                        {
                            editor = Editor.CreateEditor(target);
                            m_editors[i] = editor;

                            m_keywordsInitialized = false;
                        }

                        editor.OnInspectorGUI();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}
