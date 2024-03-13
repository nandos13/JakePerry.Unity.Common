using JakePerry.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    /// <summary>
    /// Provides a base class for 'Selector' windows, similar to Unity's Object Selector.
    /// </summary>
    public abstract class AbstractSelectorWindow : EditorWindow
    {
        private const string kSearchBarControlName = "JakePerry.Unity.AbstractSelectorWindow.SearchBarControl";
        private const string kWidthPref = "JakePerry.Unity.AbstractSelectorWindow.Width.";
        private const string kHeightPref = "JakePerry.Unity.AbstractSelectorWindow.Height.";

        private static readonly Dictionary<Type, AbstractSelectorWindow> _sharedInstances = new();

        private static int? _sendEventControlId;

        private readonly List<Substring> m_searchWords = new();

        private GUIStyle _searchBarStyle;
        private GUIStyle _searchLabelStyle;

        private ScriptableObject m_delegateView;
        private int m_controlId;
        private string m_searchFilter;
        private Vector2 m_scroll;

        private static Type GUIViewType => ReflectionEx.GetType(typeof(Editor).Assembly, "UnityEditor.GUIView");

        private static ScriptableObject CurrentGUIView
        {
            get
            {
                var prop = ReflectionEx.GetProperty(GUIViewType, "current", BindingFlags.Static | BindingFlags.Public);
                return (ScriptableObject)prop.GetValue(null);
            }
        }

        // TODO: Documentation
        public static int ControlID
        {
            get
            {
                if (_sendEventControlId.HasValue)
                {
                    return _sendEventControlId.Value;
                }
                throw new InvalidOperationException(
                    "No command event in progress. This property should only be accessed during a " +
                    "GUI command event that was sent by a child class.");
            }
        }

        protected abstract string Title { get; }

        protected int CurrentControlId => m_controlId;

        protected GUIStyle SearchBarStyle => _searchBarStyle ??= CreateSearchBarStyle();

        protected GUIStyle SearchLabelStyle => _searchLabelStyle ??= CreateSearchLabelStyle();

        protected ReadOnlyList<Substring> SearchTerms => m_searchWords;

        protected virtual GUIStyle CreateSearchBarStyle()
        {
            return new GUIStyle(EditorGUIEx.Styles.GetStyle("m_ToolbarSearchField"))
            {
                fixedHeight = 0
            };
        }

        protected virtual GUIStyle CreateSearchLabelStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel);
        }

        /// <summary>
        /// Send an event to the GUI View which opened the selector window.
        /// </summary>
        /// <remarks>
        /// Emulates UnityEditor.ObjectSelector.SendEvent.
        /// </remarks>
        protected void SendEvent(string eventName)
        {
            const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            if (m_delegateView != null)
            {
                Event e = EditorGUIUtility.CommandEvent(eventName);

                var parameterTypes = new ParamsArray<Type>(typeof(Event));
                var method = ReflectionEx.GetMethod(GUIViewType, "SendEvent", kFlags, parameterTypes);

                var args = ReflectionEx.RentArrayWithArguments(e);

                try
                {
                    _sendEventControlId = m_controlId;

                    method.Invoke(m_delegateView, args);
                }
                finally { _sendEventControlId = null; }

                ReflectionEx.ReturnArray(args);
            }
        }

        protected abstract void DrawBodyGUI();

        protected virtual void OnSearchFilterChanged() { }

        private static T GetSharedInstance<T>()
            where T : AbstractSelectorWindow
        {
            if (!_sharedInstances.TryGetValue(typeof(T), out var window) ||
                window == null)
            {
                window = CreateInstance<T>();
            }
            return (T)window;
        }

        // TODO: Documentation
        protected static T ShowWindow<T>(int controlId)
            where T : AbstractSelectorWindow
        {
            var inst = GetSharedInstance<T>();

            inst.m_delegateView = CurrentGUIView;
            inst.m_controlId = controlId;

            inst.ShowAuxWindow();

            return inst;
        }

        private void ClearSearchFilter()
        {
            bool flag = m_searchWords.Count > 0;

            m_searchFilter = null;
            m_searchWords.Clear();

            if (flag)
            {
                OnSearchFilterChanged();
            }
        }

        private void RecordWindowSize()
        {
            var rect = base.position;
            var typeGuid = this.GetType().GUID.ToString("N");

            EditorPrefs.SetFloat(kWidthPref + typeGuid, rect.width);
            EditorPrefs.SetFloat(kHeightPref + typeGuid, rect.height);
        }

        private void DrawSearchBar()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(LineHeight + 8));
            rect = rect.Pad(2, 2, 5, 5);

            var labelRect = rect;
            labelRect.width = 70f;
            rect = rect.PadLeft(labelRect.width + 20f);

            EditorGUI.LabelField(labelRect, "Search:", SearchLabelStyle);

            var textRect = rect;

            var style = SearchBarStyle;

            EditorGUI.BeginChangeCheck();
            {
                GUI.SetNextControlName(kSearchBarControlName);
                m_searchFilter = EditorGUI.TextField(textRect, m_searchFilter, style);
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_searchWords.Clear();

                if (!string.IsNullOrWhiteSpace(m_searchFilter))
                {
                    using var scope = ListPool.RentInScope(out List<Substring> splits);
                    Substring.Split(m_searchFilter, " ", splits, StringSplitOptions.RemoveEmptyEntries);
                    m_searchWords.AddRange(splits);
                }
                else
                {
                    m_searchFilter = null;
                }

                OnSearchFilterChanged();
            }
        }

        private bool SearchBoxHasFocus()
        {
            // TODO: Not happy with the gui control name solution, seems to now always take focus
            return StringComparer.Ordinal.Equals(kSearchBarControlName, GUI.GetNameOfFocusedControl());
        }

        private void HandleKeyboardInput()
        {
            var current = Event.current;
            if (current.type == EventType.KeyDown)
            {
                var key = current.keyCode;
                if ((key == KeyCode.Return || key == KeyCode.KeypadEnter) &&
                    !SearchBoxHasFocus())
                {
                    Close();
                    GUI.changed = true;
                    GUIUtility.ExitGUI();
                }
            }
        }

        protected void OnGUI()
        {
            HandleKeyboardInput();

            DrawSearchBar();

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            {
                DrawBodyGUI();
            }
            EditorGUILayout.EndScrollView();

            RecordWindowSize();

            var current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                Close();
                GUIUtility.ExitGUI();
            }
            else if (current.commandName == "UndoRedoPerformed")
            {
                Close();
                GUIUtility.ExitGUI();
            }
        }

        protected virtual void OnEnable()
        {
            ClearSearchFilter();

            m_scroll = default;

            var typeGuid = this.GetType().GUID.ToString("N");
            float w = EditorPrefs.GetFloat(kWidthPref + typeGuid, 640);
            float h = EditorPrefs.GetFloat(kHeightPref + typeGuid, 397);

            base.titleContent = new GUIContent(Title);
            base.position = new Rect(0, 0, w, h);
            base.minSize = new Vector2(200f, 335f);
            base.maxSize = new Vector2(10000f, 10000f);
            Focus();
        }

        protected virtual void OnDisable() { }
    }
}
