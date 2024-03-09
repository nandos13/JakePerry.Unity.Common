using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    // TODO: Add extra buttons next to the search bar to filter by class, struct, etc.
    // (when applicable of course, if generic constraint already enforces one, then lock it).

    [RequiresConstantRepaint]
    public sealed class TypeSelector : AbstractSelectorWindow
    {
        /// <summary>
        /// Denotes all Unity GUI command names used by the TypeSelector.
        /// </summary>
        public static class Commands
        {
            public const string SelectionUpdated = "TypeSelector_SelectedTypeUpdated";
        }

        private const float kIndent = 30;
        private const float kFoldoutSize = 20;

        private const byte kSelUpdateId = 1;

        private sealed class Map
        {
            public readonly Namespace @namespace;
            public readonly List<Type> types = new();
            public readonly AnimBool visible = new(false);
            public Map(Namespace n) { @namespace = n; }
        }

        private static readonly Dictionary<Type, (string, string)> _displayNameCache = new();

        private static readonly (Type, string)[] _builtInTypes = new (Type, string)[]
        {
            (typeof(bool), "bool"),
            (typeof(byte), "byte"),
            (typeof(sbyte), "sbyte"),
            (typeof(char), "char"),
            (typeof(float), "float"),
            (typeof(double), "double"),
            (typeof(decimal), "decimal"),
            (typeof(short), "short"),
            (typeof(ushort), "ushort"),
            (typeof(int), "int"),
            (typeof(uint), "uint"),
            (typeof(long), "long"),
            (typeof(ulong), "ulong"),
            (typeof(nint), "nint"),
            (typeof(nuint), "nuint"),
            (typeof(string), "string"),
            (typeof(object), "object")
        };

        private static readonly Color32 _namespaceBackgroundColor = new Color32(40, 40, 40, 255);
        // TODO: Use this color for built in types.
        private static readonly Color32 _builtInAliasColor = new Color32(86, 156, 214, 255);

        private static Type _selectedType;
        private static byte _commandState;

        private readonly List<Map> m_typeMap = new();
        private readonly Map m_builtInMap = new(default);
        private Map m_globalNamespaceMap;

        private GUIStyle m_namespaceStyle;
        private GUIStyle m_namespaceFullNameStyle;
        private GUIStyle m_typeNameStyle;
        private GUIStyle m_typeFullNameStyle;

        private Type m_currentSelection;

        // TODO: Documentation
        public static Type SelectedType
        {
            get
            {
                if (_commandState == kSelUpdateId)
                {
                    return _selectedType;
                }
                throw new InvalidOperationException(
                    "No type selection in progress. This property should only be accessed during the "
                    + Commands.SelectionUpdated + " command GUI event.");
            }
        }

        private GUIStyle NamespaceStyle
        {
            get
            {
                if (m_namespaceStyle is null)
                {
                    m_namespaceStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = (int)(EditorStyles.boldLabel.fontSize * 1.5f)
                    };
                }
                return m_namespaceStyle;
            }
        }

        private GUIStyle NamespaceFullNameStyle
        {
            get
            {
                if (m_namespaceFullNameStyle is null)
                {
                    m_namespaceFullNameStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = (int)(EditorStyles.label.fontSize * 1.2f),
                        fontStyle = FontStyle.Italic
                    };
                }
                return m_namespaceFullNameStyle;
            }
        }

        protected sealed override string Title => "Select Type";

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void OnRecompile()
        {
            _displayNameCache.Clear();

            var sb = StringBuilderCache.Acquire();

            foreach (var tuple in _builtInTypes)
            {
                sb.Clear();
                sb.Append('[');
                sb.Append(tuple.Item1.FullName);
                sb.Append(']');

                _displayNameCache[tuple.Item1] = (tuple.Item2, sb.ToString());
            }

            StringBuilderCache.Release(sb);
        }

        private static int MapSorter(Map x, Map y)
        {
            return x.@namespace.CompareTo(y.@namespace);
        }

        private static int TypeSorter(Type x, Type y)
        {
            return StringComparer.Ordinal.Compare(x.Name, y.Name);
        }

        private static void HandleNestedTypeDisplayName(Type t, StringBuilder sb)
        {
            do
            {
                t = t.DeclaringType;

                sb.Insert(0, '+');
                sb.Insert(0, t.Name);
            }
            while (t.IsNested);
        }

        private static (string name, string full) GetDisplayNames(Type t)
        {
            // TODO: Generic names dont show arg names List`1

            if (t is null)
            {
                return ("None", null);
            }

            if (!_displayNameCache.TryGetValue(t, out var result))
            {
                var sb = StringBuilderCache.Acquire();

                if (t.IsNested)
                {
                    sb.Insert(0, t.Name);
                    HandleNestedTypeDisplayName(t, sb);

                    result.Item1 = sb.ToString();
                }
                else
                {
                    result.Item1 = t.Name;
                }

                result.Item2 = null;
                if (!string.IsNullOrEmpty(t.Namespace))
                {
                    sb.Clear();
                    sb.Append('[');
                    sb.Append(t.Namespace);
                    sb.Append('.');
                    sb.Append(result.Item1);
                    sb.Append(']');

                    result.Item2 = sb.ToString();
                }

                StringBuilderCache.Release(sb);

                _displayNameCache[t] = result;
            }

            return result;
        }

        private bool FindMap(Namespace @namespace, out Map map)
        {
            foreach (var m in m_typeMap)
                if (m.@namespace.Equals(@namespace))
                {
                    map = m;
                    return true;
                }

            map = default;
            return false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_currentSelection = null;
        }

        private void SendSelectEventAndClose()
        {
            try
            {
                _selectedType = m_currentSelection;
                _commandState = kSelUpdateId;

                SendEvent(Commands.SelectionUpdated);
            }
            finally { _commandState = 0; _selectedType = null; }

            Close();
            GUIUtility.ExitGUI();
        }

        private void DrawNamespaceHeader(string name, string fullName, AnimBool visible, int indentLevel)
        {
            var style = NamespaceStyle;

            var content = GetTempContent(name);
            var rect = EditorGUILayout.GetControlRect(false, style.CalcHeight(content, Screen.width), style);

            var current = Event.current;
            if (current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, _namespaceBackgroundColor);
            }

            rect = rect.PadLeft(indentLevel * kIndent);

            var foldoutRect = rect.WithSize(kFoldoutSize, kFoldoutSize);
            rect = rect.PadLeft(foldoutRect.width + Spacing);

            bool hover = rect.Contains(current.mousePosition);
            if (current.type == EventType.Repaint)
            {
                // TODO: Nicer foldout visuals
                EditorGUI.Foldout(foldoutRect, visible.value, GUIContent.none);
                style.Draw(rect, content, hover, false, false, false);

                if (!string.IsNullOrEmpty(fullName) &&
                    !StringComparer.Ordinal.Equals(fullName, name))
                {
                    style.CalcMinMaxWidth(content, out float nameWidth, out _);
                    rect = rect.PadLeft(nameWidth + Spacing);

                    content.text = fullName;
                    NamespaceFullNameStyle.Draw(rect, content, hover, false, false, false);
                }
            }
            else if (current.type == EventType.MouseDown && hover)
            {
                // TODO: Consider auto collapsing everything else for performance?

                current.Use();
                visible.target = !visible.value;
            }

            // TODO: Figure out keyboard focus, support navigating with arrows.
            //       Left/right to expand collapse namespaces
        }

        private void DrawType(Type t, int indentLevel, Color32? forceColor = null)
        {
            var content = TempContent;
            (string name, string full) = GetDisplayNames(t);

            content.text = name;
            m_typeNameStyle ??= new GUIStyle(EditorStyles.label);
            m_typeNameStyle.CalcMinMaxWidth(content, out float nameWidth, out _);

            float fullNameWidth = 0f;
            if (full is not null)
            {
                content.text = full;
                m_typeFullNameStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Italic
                };
                m_typeFullNameStyle.CalcMinMaxWidth(content, out fullNameWidth, out _);
            }

            float padLeft = indentLevel * kIndent + kFoldoutSize + Spacing;
            float minWidth = padLeft + nameWidth + fullNameWidth + Spacing * 2f;

            var rect = EditorGUILayout.GetControlRect(false, LineHeight, GUILayout.MinWidth(minWidth));
            var rowRect = rect;

            rect = rect.PadLeft(padLeft);

            var current = Event.current;
            bool hover = rect.Contains(current.mousePosition);
            bool active = m_currentSelection == t;

            if (current.type == EventType.Repaint)
            {
                var bgStyle = ReorderableList.defaultBehaviours.elementBackground;
                bgStyle.Draw(rowRect, GUIContent.none, hover, active, active, false);

                if (forceColor.HasValue)
                {
                    m_typeNameStyle.normal.textColor = forceColor.Value;
                }
                else
                {
                    // TODO: Colors for class/struct/interface
                    m_typeNameStyle.normal.textColor = Color.red;
                }

                m_typeNameStyle.hover.textColor = Color.Lerp(m_typeNameStyle.normal.textColor, Color.white, 0.4f);
                m_typeNameStyle.active.textColor = m_typeNameStyle.normal.textColor;

                content.text = name;
                m_typeNameStyle.Draw(rect, content, hover, active, false, false);

                if (full is not null)
                {
                    rect = rect.PadLeft(nameWidth + Spacing);

                    content.text = full;
                    m_typeFullNameStyle.Draw(rect, content, hover, active, false, false);
                }
            }
            else if (current.type == EventType.MouseDown)
            {
                if (hover && current.button == 0)
                {
                    m_currentSelection = t;

                    if (current.clickCount == 2)
                    {
                        SendSelectEventAndClose();
                    }
                    else
                    {
                        GUIUtility.ExitGUI();
                    }
                    current.Use();
                }
            }
        }

        private void DrawTypes(Map map, int indentLevel)
        {
            var visible = map.visible;
            if (visible.isAnimating || visible.value)
            {
                EditorGUILayout.BeginFadeGroup(visible.faded);

                for (int i = 0; i < map.@namespace.NestedCount; ++i)
                {
                    var child = map.@namespace.GetNestedNamespace(i);
                    if (FindMap(child, out Map other))
                    {
                        DrawNamespaceHeader(child.Name, child.FullName, other.visible, indentLevel + 1);
                        DrawTypes(other, indentLevel + 1);
                    }
                }

                Color32? forceColor = map == m_builtInMap ? _builtInAliasColor : null;

                foreach (var t in map.types)
                {
                    DrawType(t, indentLevel, forceColor);
                }

                EditorGUILayout.EndFadeGroup();
            }
        }

        protected sealed override void DrawBodyGUI()
        {
            // TODO: Color for None type
            DrawType(null, 0, forceColor: Color.green);

            if (m_builtInMap.types.Count > 0)
            {
                DrawNamespaceHeader("Built In Types", null, m_builtInMap.visible, 0);
                DrawTypes(m_builtInMap, 0);
            }

            if (m_globalNamespaceMap.types.Count > 0 ||
                m_globalNamespaceMap.@namespace.NestedCount > 0)
            {
                DrawNamespaceHeader("Global", null, m_globalNamespaceMap.visible, 0);
                DrawTypes(m_globalNamespaceMap, 0);
            }
        }

        private void Setup(Type current)
        {
            m_typeMap.Clear();
            m_builtInMap.types.Clear();

            foreach (var type in TypeCache.GetTypesDerivedFrom<object>())
            {
                // TODO: Type validation depending on restrictions

                var @namespace = NamespaceCache.GetNamespace(type);

                // TODO: Check if it's the global namespace, add it to another list instead.

                Map map;
                foreach (var m in m_typeMap)
                    if (m.@namespace.Equals(@namespace))
                    {
                        map = m;
                        goto ADD_TYPE_TO_MAP;
                    }

                map = new Map(@namespace);
                m_typeMap.Add(map);

            ADD_TYPE_TO_MAP:
                map.types.Add(type);
            }

            var comparison = new Comparison<Type>(TypeSorter);
            foreach (var m in m_typeMap)
            {
                m.types.Sort(comparison);
            }

            m_typeMap.Sort(new Comparison<Map>(MapSorter));

            foreach (var tuple in _builtInTypes)
            {
                // TODO: Validate built in types
                m_builtInMap.types.Add(tuple.Item1);
            }

            m_globalNamespaceMap = new Map(NamespaceCache.GetGlobalNamespace());

            m_currentSelection = current;
        }

        public static void OpenTypeSelector(int controlId, Type current)
        {
            var window = ShowWindow<TypeSelector>(controlId);
            window.Setup(current);

            // TODO: Should this auto focus and expand to show the current selection
        }
    }
}
