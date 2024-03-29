using JakePerry.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private const float kIndent = 30;
        private const float kFoldoutSize = 20;

        private const float kHoverLerpTerm = 0.3f;

        /// <summary>
        /// Denotes the name of the Unity GUI command sent when a type is selected.
        /// </summary>
        public const string SelectionUpdatedCommand = "TypeSelector_SelectedTypeUpdated";

        private enum FilterState
        {
            /// <summary>
            /// Namespace is not affected by the search filter.
            /// </summary>
            None = 0,

            /// <summary>
            /// Some types in the namespace are filtered, but some are still available.
            /// </summary>
            Some = 1,

            /// <summary>
            /// All types in the namespace are filtered and as such it can be skipped.
            /// </summary>
            All = 2
        }

        private readonly struct TypeDisplayNames
        {
            public readonly string name;
            public readonly string filter;
            public readonly string braced;

            public TypeDisplayNames(string name, string filter, string braced)
            {
                this.name = name;
                this.filter = filter;
                this.braced = braced;
            }
        }

        // TODO: Kill the Map class, maintain separate lists. Use BinarySearch, match index.
        private sealed class Map
        {
            public readonly Namespace @namespace;
            public readonly Type[] types;
            public readonly AnimBool visible = new(false);
            public FilterState filterState;

            public Map(Namespace ns, Type[] types)
            {
                @namespace = ns;
                this.types = types;
            }
        }

        private static readonly Dictionary<Type, TypeDisplayNames> _displayNameCache = new();

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

        private static Type _selectedType;
        private static bool _invokingTypeSelectCommand;

        private readonly HashSet<Type> m_typesMatchingCurrentFilter = new();
        private readonly List<Map> m_typeMap = new(capacity: 2048);

        private Map m_builtInMap;
        private Map m_globalNamespaceMap;

        private GUIStyle m_namespaceStyle;
        private GUIStyle m_namespaceFullNameStyle;
        private GUIStyle m_typeNameStyle;
        private GUIStyle m_typeFullNameStyle;

        private Type m_currentSelection;

        // TODO: Consider adding a config file for specifying render colors?
        private static Color32 DefaultAliasColor => new Color32(86, 156, 214, 255);
        private static Color32 DefaultClassColor => new Color32(78, 201, 176, 255);
        private static Color32 DefaultInterfaceColor => new Color32(184, 215, 163, 255);
        private static Color32 DefaultStructColor => new Color32(134, 198, 145, 255);
        private static Color32 NamespaceBackgroundColor => new Color32(40, 40, 40, 255);
        private static Color32 White32 => new Color32(255, 255, 255, 255);

        /// <summary>
        /// The <see cref="Type"/> that was selected by the user.
        /// This is only available during the "Selection Updated" GUI command event.
        /// </summary>
        /// <seealso cref="SelectionUpdatedCommand"/>
        public static Type SelectedType
        {
            get
            {
                if (_invokingTypeSelectCommand)
                {
                    return _selectedType;
                }
                throw new InvalidOperationException(
                    "No type selection in progress. This property should only be accessed during the "
                    + SelectionUpdatedCommand + " command GUI event.");
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
                string name = tuple.Item2;

                sb.Clear();
                sb.Append(name);
                sb.Append(' ');
                sb.Append(tuple.Item1.FullName);

                string filter = sb.ToString();

                sb.Clear();
                sb.Append('[');
                sb.Append(tuple.Item1.FullName);
                sb.Append(']');

                string braced = sb.ToString();

                _displayNameCache[tuple.Item1] = new(name, filter, braced);
            }

            StringBuilderCache.Release(sb);
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

        private static TypeDisplayNames GetDisplayNames(Type t)
        {
            // TODO: Generic names dont show arg names List`1
            //       Be aware of recursion if generic arg is decalring type (ie. List<List<int>>)

            if (t is null)
            {
                return new("None", null, null);
            }

            if (!_displayNameCache.TryGetValue(t, out var result))
            {
                var sb = StringBuilderCache.Acquire();

                string name, full, braced;

                if (t.IsNested)
                {
                    sb.Insert(0, t.Name);
                    HandleNestedTypeDisplayName(t, sb);

                    name = sb.ToString();
                }
                else
                {
                    name = t.Name;
                }

                if (!string.IsNullOrEmpty(t.Namespace))
                {
                    sb.Clear();
                    sb.Append(t.Namespace);
                    sb.Append('.');
                    sb.Append(name);

                    full = sb.ToString();

                    sb.Insert(0, '[');
                    sb.Append(']');

                    braced = sb.ToString();
                }
                else
                {
                    full = name;
                    braced = null;
                }

                StringBuilderCache.Release(sb);

                _displayNameCache[t] = result = new(name, full, braced);
            }

            return result;
        }

        private static bool IgnoreType(Type t)
        {
            if (t.IsDefined(typeof(CompilerGeneratedAttribute), false))
                return true;

            if (t.FullName.Contains("<PrivateImplementationDetails>", StringComparison.Ordinal))
                return true;

            return false;
        }

        private static bool MatchSearchTerms(string value, ReadOnlyList<Substring> searchTerms)
        {
            var span = value.AsSpan();
            foreach (var term in searchTerms)
                if (!span.Contains(term.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

            return true;
        }

        private void CollapseChildren(Map map)
        {
            var @namespace = map.@namespace;
            foreach (var map2 in m_typeMap)
                if (map2.@namespace.IsDescendantOf(@namespace))
                {
                    map2.visible.target = false;
                }
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
                _invokingTypeSelectCommand = true;

                SendEvent(SelectionUpdatedCommand);
            }
            finally { _invokingTypeSelectCommand = false; _selectedType = null; }

            Close();
            GUIUtility.ExitGUI();
        }

        private void DrawNamespaceHeader(Map map, int indentLevel)
        {
            string name, fullName = null;
            if (map == m_builtInMap) name = "Built in";
            else if (map == m_globalNamespaceMap) name = "Global";
            else
            {
                name = map.@namespace.Name;
                fullName = map.@namespace.FullName;
            }

            var visible = map.visible;

            var style = NamespaceStyle;

            var content = GetTempContent(name);
            var rect = EditorGUILayout.GetControlRect(false, style.CalcHeight(content, Screen.width), style);

            var current = Event.current;

            bool hover = rect.Contains(current.mousePosition);
            if (current.type == EventType.Repaint)
            {
                var color = NamespaceBackgroundColor;
                if (hover) color = Color32.Lerp(color, White32, kHoverLerpTerm);

                EditorGUI.DrawRect(rect, color);
            }

            rect = rect.PadLeft(indentLevel * kIndent);

            var foldoutRect = rect.WithSize(kFoldoutSize, kFoldoutSize);
            rect = rect.PadLeft(foldoutRect.width + Spacing);

            if (current.type == EventType.Repaint)
            {
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
                bool newVisibleState = !visible.value;

                current.Use();
                visible.target = newVisibleState;

                // Collapsing a namespace also collapses all child namespaces
                if (!newVisibleState)
                {
                    CollapseChildren(map);
                }
            }

            // TODO: Figure out keyboard focus, support navigating with arrows.
            //       Left/right to expand collapse namespaces
            //       Will require GUIUtility.keyboardControl
            //       up/down arrows will need to cycle through entries, unsure how thats done
        }

        private void DrawType(Type t, int indentLevel, Color32? forceColor = null)
        {
            var content = TempContent;
            var displayNames = GetDisplayNames(t);

            content.text = displayNames.name;
            m_typeNameStyle ??= new GUIStyle(EditorStyles.label);
            m_typeNameStyle.CalcMinMaxWidth(content, out float nameWidth, out _);

            float fullNameWidth = 0f;
            if (displayNames.braced is not null)
            {
                content.text = displayNames.braced;
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

                Color32 color32;
                if (forceColor.HasValue)
                {
                    color32 = forceColor.Value;
                }
                else if (t is not null)
                {
                    if (t.IsValueType) color32 = DefaultStructColor;
                    else color32 = t.IsInterface ? DefaultInterfaceColor : DefaultClassColor;
                }
                else
                {
                    color32 = new Color32(200, 200, 200, 255);
                }

                m_typeNameStyle.normal.textColor = m_typeNameStyle.active.textColor = color32;
                m_typeNameStyle.hover.textColor = Color.Lerp(color32, White32, kHoverLerpTerm);

                content.text = displayNames.name;
                m_typeNameStyle.Draw(rect, content, hover, active, false, false);

                if (displayNames.braced is not null)
                {
                    rect = rect.PadLeft(nameWidth + Spacing);

                    content.text = displayNames.braced;
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

        private bool AnyTypesAvailable(Map map)
        {
            switch (map.filterState)
            {
                case FilterState.None:
                    {
                        if ((map.types?.Length ?? 0) > 0) return true;
                        break;
                    }

                case FilterState.Some:
                    {
                        foreach (var t in map.types)
                            if (m_typesMatchingCurrentFilter.Contains(t))
                            {
                                return true;
                            }
                        break;
                    }
            }

            return false;
        }

        private void DrawMap(Map map, int indentLevel)
        {
            // TODO: Filter by 'jp', the Unity namespace shows but is empty when expanded.

            // Skip if no types available in this namespace or any descendant namespaces.
            if (!AnyTypesAvailable(map))
            {
                foreach (var map2 in m_typeMap)
                    if (map2.@namespace.IsDescendantOf(map.@namespace) &&
                        AnyTypesAvailable(map2))
                    {
                        goto PROCEED;
                    }
                return;
            }

        PROCEED:
            DrawNamespaceHeader(map, indentLevel);

            var visible = map.visible;
            if (visible.isAnimating || visible.value)
            {
                EditorGUILayout.BeginFadeGroup(visible.faded);

                foreach (var map2 in m_typeMap)
                    if (map2.@namespace.IsChildOf(map.@namespace))
                    {
                        DrawMap(map2, indentLevel + 1);
                    }

                if (map.filterState != FilterState.All)
                {
                    Color32? forceColor = map == m_builtInMap ? DefaultAliasColor : null;

                    bool checkFilterPerType = map.filterState == FilterState.Some;
                    foreach (var t in map.types)
                    {
                        if (!checkFilterPerType || m_typesMatchingCurrentFilter.Contains(t))
                        {
                            DrawType(t, indentLevel, forceColor);
                        }
                    }
                }

                EditorGUILayout.EndFadeGroup();
            }
        }

        private void UpdateFilterState(Map map, in ReadOnlyList<Substring> searchTerms)
        {
            // First pass is an optimization. If a namespace itself matches the search filter,
            // then all types in the namespace must also match.
            if (map == m_builtInMap || map == m_globalNamespaceMap)
            {
                map.filterState = FilterState.Some;
            }
            else
            {
                map.filterState = MatchSearchTerms(map.@namespace.FullName, searchTerms)
                    ? FilterState.None
                    : FilterState.Some;
            }

            // Second pass checks per-type.
            if (map.filterState != FilterState.None)
            {
                bool anyAvailable = false;
                foreach (var t in map.types)
                {
                    var displayNames = GetDisplayNames(t);
                    if (MatchSearchTerms(displayNames.filter, searchTerms))
                    {
                        anyAvailable = true;
                        m_typesMatchingCurrentFilter.Add(t);
                    }
                }

                map.filterState = anyAvailable ? FilterState.Some : FilterState.All;
            }
        }

        protected sealed override void OnSearchFilterChanged()
        {
            base.OnSearchFilterChanged();

            m_typesMatchingCurrentFilter.Clear();

            var searchTerms = base.SearchTerms;

            // If there is no search filter, everything is available.
            if (searchTerms.Count == 0)
            {
                m_builtInMap.filterState = FilterState.None;

                foreach (var map in m_typeMap)
                {
                    map.filterState = FilterState.None;
                }
            }
            else
            {
                UpdateFilterState(m_builtInMap, searchTerms);

                foreach (var map in m_typeMap)
                {
                    UpdateFilterState(map, searchTerms);
                }
            }
        }

        protected sealed override void DrawBodyGUI()
        {
            DrawType(null, 0);

            DrawMap(m_builtInMap, 0);
            DrawMap(m_globalNamespaceMap, 0);
        }

        private void Setup(Type current)
        {
            const int kBufferSize = 16;

            m_typeMap.Clear();

            var nsList = new List<string>(capacity: 512);
            var buffers = new List<LinkedList<FixedSizeBuffer<Type>>>(capacity: 512);

            var nsComparer = StringComparer.Ordinal;

            // TODO: Async this, put window in a loading state, show something in the gui instead of freeze.

            // Note:
            // Ideally this code would use Unity's TypeCache API here to take advantage of its
            // performance benefits, however the API does not expose a complete collection of all
            // types in the current AppDomain. As such, we must manually enumerate all types from
            // all assemblies, and suffer the overhead.
            // Forum post:
            // https://forum.unity.com/threads/typecache-does-not-scan-all-assemblies-in-the-current-appdomain.1558037/#post-9698186
            // Issue tracker:
            // https://issuetracker.unity3d.com/issues/not-all-assemblies-are-found-in-the-current-appdomain-when-scanning-with-typecache
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
            {
                if (IgnoreType(type)) continue;

                // TODO: Type validation depending on restrictions

                var ns = type.Namespace;
                int index = nsList.BinarySearch(ns, nsComparer);

                LinkedList<FixedSizeBuffer<Type>> linkedList;
                if (index > -1)
                {
                    linkedList = buffers[index];
                }
                else
                {
                    index = ~index;
                    nsList.Insert(index, ns);
                    buffers.Insert(index, linkedList = new());
                    linkedList.AddFirst(new FixedSizeBuffer<Type>(kBufferSize));
                }

                if (!linkedList.Last.Value.Add(type))
                {
                    linkedList.AddLast(new FixedSizeBuffer<Type>(kBufferSize)).Value.Add(type);
                }
            }

            // Type.Namespace property returns null for types in the global namespace.
            // The Namespace.GetNamespace method resolves null as 'None';
            // An empty string corresponds to the global namespace.
            // Switch null to an empty string here.
            int globalIndex = nsList.IndexOf(null);
            if (globalIndex > -1) nsList[globalIndex] = string.Empty;

            // Consolidate linked lists into contiguous buffers
            for (int i = 0; i < nsList.Count; ++i)
            {
                var ns = nsList[i];
                var linkedList = buffers[i];

                var contiguousBuffer = new Type[kBufferSize * (linkedList.Count - 1) + linkedList.Last.Value.Count];
                var n = linkedList.First;
                int o = 0;
                do
                {
                    n.Value.CopyTo(contiguousBuffer, o);
                    o += n.Value.Count;
                    n = n.Next;
                }
                while (n is not null);

                m_typeMap.Add(new Map(Namespace.GetNamespace(ns), contiguousBuffer));
            }

            var comparison = new Comparison<Type>(TypeSorter);
            foreach (var m in m_typeMap)
            {
                Array.Sort(m.types, comparison);
            }
            // TODO: Thread the above array sorting, it's a bottleneck.
            // Can I sort these using job system? No reference types (cant pass Type).
            // Can perhaps capture name (char*) and original index in a struct,
            // perform sort in Job, then match results in c#. This will need some serious testing
            // to make sure its actually worth the code bloat and gives significant speed bonus.

            // Ensure intermediate namespaces are always included, even if
            // they don't define any types.
            for (int i = 0; i < nsList.Count; ++i)
            {
                var ns = Namespace.GetNamespace(nsList[i]);
                while (!ns.IsRoot)
                {
                    ns = ns.BaseNamespace;
                    int baseNamespaceIndex = nsList.BinarySearch(ns.FullName);
                    if (baseNamespaceIndex < 0)
                    {
                        int index = ~baseNamespaceIndex;
                        nsList.Insert(index, ns.FullName);
                        m_typeMap.Insert(index, new Map(ns, Array.Empty<Type>()));

                        if (index <= i) ++i;
                    }
                }
            }

            globalIndex = nsList.IndexOf(string.Empty);
            Debug.Assert(globalIndex > -1, "Global Namespace not present.");

            m_globalNamespaceMap = m_typeMap[globalIndex];

            var bTypes = new List<Type>(_builtInTypes.Length);
            foreach (var tuple in _builtInTypes)
            {
                // TODO: Validate built in types

                bTypes.Add(tuple.Item1);
            }
            m_builtInMap = new Map(Namespace.None, bTypes.ToArray());

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
