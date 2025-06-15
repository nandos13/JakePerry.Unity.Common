using JakePerry.Collections;
using JakePerry.Reflection;
using JakePerry.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [RequiresConstantRepaint]
    public sealed class TypeSelector : AbstractSelectorWindow
    {
        private const string kBuiltInTypesIdentifier = "__typeselector_builtin";

        private const float kIndent = 30;
        private const float kFoldoutSize = 20;

        private const float kHoverLerpTerm = 0.3f;

        private const char kCheckMark = (char)0x2713;

        /// <summary>
        /// Denotes the name of the Unity GUI command sent when a type is selected.
        /// </summary>
        public const string SelectionUpdatedCommand = "TypeSelector_SelectedTypeUpdated";

        private enum FilterState { Unfiltered = 0, Partial = 1, Hidden = 2 }

        private sealed class ScanWorkItem
        {
            private readonly Assembly m_assembly;

            private string[] m_namespaces;
            private ReadOnlyArray<Type>[] m_namespaceTypes;

            public Assembly Assembly => m_assembly;

            public (string[] namespaces, ReadOnlyArray<Type>[] types) Result => (m_namespaces, m_namespaceTypes);

            public ScanWorkItem(Assembly assembly)
            {
                m_assembly = assembly;
            }

            public void SetResult(string[] namespaces, ReadOnlyArray<Type>[] namespaceTypes)
            {
                m_namespaces = namespaces;
                m_namespaceTypes = namespaceTypes;
            }
        }

        private sealed class State
        {
            public readonly Type[] types;
            public readonly BitArray hidden;
            public AnimBool expanded;
            public bool allHidden = false;

            public State(Type[] types)
            {
                this.types = types;
                hidden = new BitArray(types.Length);
            }
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

        private sealed class GenericConstraintCheck
        {
            private readonly Type m_genericParameter;

            public GenericConstraintCheck(Type genericParameter) { m_genericParameter = genericParameter; }

            public bool Incompatible(Type t)
            {
                // TODO: Implementation...
                // Consider type:
                // Class<T, U>
                //  where T : IEquatable<IComparer<U>>
                //  where U : T
                //
                // Both restrict each other, and we can't validate one at a time.
                // If constraints are only one way (ie the T constraint was removed),
                // we can disable editing the U parameter until T is fully qualified.
                // In cases where its bi-directional, just allow one to be assigned
                // and sort shit out after that?
                // Perhaps a check in the type drawer that checks if a generic type is
                // correctly defined?

                // The following two members will be useful...
                //t.GetGenericParameterConstraints;
                //t.GenericParameterAttributes;



                // TODO: Remove this later. This approach only works for generics
                // with one parameter... Gonna need something a lot more complex :) :) :)
                try { ReflectionEx.MakeGenericType(m_genericParameter.DeclaringType, new(t)); }
                catch { return true; }
                return false;
            }
        }

        private static readonly object _scanLock = new object();

        private static readonly List<string> _appDomainNamespaces = new(capacity: 2048);
        private static readonly List<List<Type>> _typesInNamespace = new(capacity: 2048);
        private static readonly HashSet<Assembly> _scannedAssemblies = new();

        private static readonly Dictionary<Type, TypeDisplayNames> _displayNameCache = new();

        private static readonly Type[] _builtInTypes = new Type[]
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(nint),
            typeof(nuint),
            typeof(string),
            typeof(object),
        };

        private static Type _selectedType;
        private static bool _invokingTypeSelectCommand;

        private string[] m_namespaceMap;
        private State[] m_stateArray;

        private GUIStyle m_namespaceStyle;
        private GUIStyle m_namespaceFullNameStyle;
        private GUIStyle m_typeNameStyle;
        private GUIStyle m_typeFullNameStyle;

        private Type m_currentSelection;

        private bool m_setupComplete;
        private CancellationTokenSource m_cancelSource;
        private readonly List<string> m_setupHintLines = new();

        private static Color32 NamespaceBackgroundColor => new Color32(40, 40, 40, 255);
        private static Color32 White32 => new Color32(255, 255, 255, 255);

        private static StringComparer NamespaceComparer => StringComparer.OrdinalIgnoreCase;

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
            lock (_scanLock)
            {
                _scannedAssemblies.Clear();
                _typesInNamespace.Clear();
                _appDomainNamespaces.Clear();
            }

            _displayNameCache.Clear();

            var sb = StringBuilderCache.Acquire();

            foreach (var t in _builtInTypes)
            {
                string name = CompilerAliases.GetAlias(t);

                sb.Clear();
                sb.Append(name);
                sb.Append(' ');
                sb.Append(t.FullName);

                string filter = sb.ToString();

                sb.Clear();
                sb.Append('[');
                sb.Append(t.FullName);
                sb.Append(']');

                string braced = sb.ToString();

                _displayNameCache[t] = new(name, filter, braced);
            }

            StringBuilderCache.Release(sb);
        }

        private static int TypeSorter(Type x, Type y)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
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

                string name, filter, braced;

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

                    filter = sb.ToString();

                    sb.Insert(0, '[');
                    sb.Append(']');

                    braced = sb.ToString();
                }
                else
                {
                    filter = name;
                    braced = null;
                }

                StringBuilderCache.Release(sb);

                _displayNameCache[t] = result = new(name, filter, braced);
            }

            return result;
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

        private void CollapseChildren(string namespc)
        {
            int index = Array.BinarySearch(m_namespaceMap, namespc, NamespaceComparer);

            for (int i = index + 1; i < m_namespaceMap.Length; ++i)
            {
                // Namespace map is sorted. All descendants will follow the current namespace in the list.
                if (!m_namespaceMap[i].StartsWith(namespc, StringComparison.Ordinal))
                {
                    break;
                }

                var expanded = m_stateArray[i].expanded;
                if (expanded is not null)
                {
                    expanded.target = false;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_currentSelection = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Put the window into an uninitialized state
            m_namespaceMap = null;
            m_stateArray = null;

            m_currentSelection = null;
            m_setupComplete = false;

            m_cancelSource?.Cancel();
            m_cancelSource = null;
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

        private bool DrawNamespaceHeader(string namespc, int indentLevel, bool expanded)
        {
            string name;
            if (namespc == kBuiltInTypesIdentifier) name = "Built in";
            else if (string.IsNullOrEmpty(namespc)) name = "Global";
            else name = NamespaceUtil.GetShortName(namespc);

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
                EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none);
                style.Draw(rect, content, hover, false, false, false);

                if (!string.IsNullOrEmpty(namespc) &&
                    namespc != kBuiltInTypesIdentifier &&
                    !StringComparer.Ordinal.Equals(namespc, name))
                {
                    style.CalcMinMaxWidth(content, out float nameWidth, out _);
                    rect = rect.PadLeft(nameWidth + Spacing);

                    content.text = namespc;
                    NamespaceFullNameStyle.Draw(rect, content, hover, false, false, false);
                }
            }
            else if (current.type == EventType.MouseDown && hover)
            {
                current.Use();
                return true;
            }

            // TODO: Figure out keyboard focus, support navigating with arrows.
            //       Left/right to expand collapse namespaces
            //       Will require GUIUtility.keyboardControl
            //       up/down arrows will need to cycle through entries, unsure how thats done

            return false;
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
                    if (t.IsValueType) color32 = CodeDisplayStyleConfig.StructColor;
                    else if (t.IsInterface) color32 = CodeDisplayStyleConfig.InterfaceColor;
                    else color32 = CodeDisplayStyleConfig.ClassColor;
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

        private void DrawMap(int index, int indentLevel)
        {
            var namespc = m_namespaceMap[index];
            var state = m_stateArray[index];
            bool isGlobal = namespc.Length == 0;

            // Skip if no types available in this namespace or any descendant namespaces.
            if (state.allHidden)
            {
                for (int i = index + 1; i < m_namespaceMap.Length; ++i)
                {
                    // Namespace map is sorted. All descendants will follow the current namespace in the list.
                    if (!m_namespaceMap[i].StartsWith(namespc, StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (!m_stateArray[i].allHidden)
                    {
                        goto PROCEED;
                    }
                }
                return;
            }

        PROCEED:
            var expand = state.expanded;
            if (DrawNamespaceHeader(namespc, indentLevel, expand?.value ?? false))
            {
                state.expanded ??= new(false);
                expand = state.expanded;

                bool newVisibleState = !expand.value;

                expand.target = newVisibleState;

                // Collapsing a namespace also collapses all child namespaces
                if (!newVisibleState)
                {
                    CollapseChildren(namespc);
                }
            }

            if (expand is not null && (expand.isAnimating || expand.value))
            {
                EditorGUILayout.BeginFadeGroup(expand.faded);

                for (int i = index + 1; i < m_namespaceMap.Length; ++i)
                {
                    // Namespace map is sorted. All descendants will follow the current namespace in the list.
                    if (!m_namespaceMap[i].StartsWith(namespc, StringComparison.Ordinal))
                    {
                        break;
                    }

                    // Don't draw the built-in collection as a child of any namespace
                    if (m_namespaceMap[i] == kBuiltInTypesIdentifier) continue;

                    bool isChild = isGlobal
                        ? m_namespaceMap[i].LastIndexOf('.') == -1
                        : m_namespaceMap[i].LastIndexOf('.') == namespc.Length;

                    if (isChild)
                    {
                        DrawMap(i, indentLevel + 1);
                    }
                }

                if (!state.allHidden)
                {
                    Color32? forceColor = namespc == kBuiltInTypesIdentifier
                        ? CodeDisplayStyleConfig.AliasColor
                        : null;

                    for (int i = 0; i < state.types.Length; ++i)
                    {
                        if (!state.hidden[i])
                        {
                            DrawType(state.types[i], indentLevel, forceColor);
                        }
                    }
                }

                EditorGUILayout.EndFadeGroup();
            }
        }

        private void UpdateFilterState(int index, in ReadOnlyList<Substring> searchTerms)
        {
            string namespc = m_namespaceMap[index];
            var state = m_stateArray[index];

            if (string.IsNullOrEmpty(namespc) ||
                StringComparer.Ordinal.Equals(namespc, kBuiltInTypesIdentifier) ||
                !MatchSearchTerms(namespc, searchTerms))
            {
                bool any = false;
                for (int i = 0; i < state.types.Length; ++i)
                {
                    var displayNames = GetDisplayNames(state.types[i]);
                    bool match = MatchSearchTerms(displayNames.filter, searchTerms);
                    any |= match;
                    state.hidden[i] = !match;
                }

                state.allHidden = !any;
            }
            else
            {
                state.hidden.SetAll(false);
                state.allHidden = false;
            }
        }

        protected sealed override void OnSearchFilterChanged()
        {
            base.OnSearchFilterChanged();

            var searchTerms = base.SearchTerms;

            // If there is no search filter, everything is available.
            if (searchTerms.Count == 0)
            {
                foreach (var state in m_stateArray)
                {
                    state.hidden.SetAll(false);
                    state.allHidden = false;
                }
            }
            else
            {
                for (int i = 0; i < m_namespaceMap.Length; ++i)
                {
                    UpdateFilterState(i, searchTerms);
                }
            }
        }

        protected override void DrawHeaderGUI()
        {
            using (new EditorGUI.DisabledScope(disabled: !m_setupComplete))
            {
                // This draws the search bar
                base.DrawHeaderGUI();
            }

            // TODO: Text hinting at current generic constraints

            // TODO: Add extra buttons next to the search bar to filter by class, struct, etc.
            // (when applicable of course, if generic constraint already enforces one, then lock it).
            // Try using a EditorGUILayout.BeginHorizontal() call to avoid refactoring the search bar method
            // and how it gets the control rect.
        }

        protected sealed override void DrawBodyGUI()
        {
            if (!m_setupComplete)
            {
                using (new EditorGUI.DisabledScope(disabled: true))
                {
                    foreach (var hintLine in m_setupHintLines)
                    {
                        var hint = GetTempContent(hintLine);
                        EditorGUILayout.LabelField(hint, EditorStyles.boldLabel);
                    }
                }
                return;
            }

            DrawType(null, 0);

            int index = Array.BinarySearch(m_namespaceMap, kBuiltInTypesIdentifier, NamespaceComparer);
            if (index > -1)
            {
                DrawMap(index, 0);
            }

            index = Array.BinarySearch(m_namespaceMap, string.Empty, NamespaceComparer);
            if (index > -1)
            {
                DrawMap(index, 0);
            }
        }

        private static bool IgnoreType(Type t)
        {
            if (t.IsDefined(typeof(CompilerGeneratedAttribute), false))
                return true;

            if (t.FullName.Contains("<PrivateImplementationDetails>", StringComparison.Ordinal))
                return true;

            return false;
        }

        private static void AddTypeToBuffer(
            string namespc,
            Type type,
            int bufferSize,
            List<string> namespaceMap,
            List<LinkedList<FixedSizeBuffer<Type>>> buffers)
        {
            int index = namespaceMap.BinarySearch(namespc, NamespaceComparer);

            LinkedList<FixedSizeBuffer<Type>> linkedList;
            if (index > -1)
            {
                linkedList = buffers[index];
            }
            else
            {
                index = ~index;
                namespaceMap.Insert(index, namespc);
                buffers.Insert(index, linkedList = new());
                linkedList.AddFirst(new FixedSizeBuffer<Type>(bufferSize));
            }

            if (!linkedList.Last.Value.Add(type))
            {
                linkedList.AddLast(new FixedSizeBuffer<Type>(bufferSize)).Value.Add(type);
            }
        }

        private static void ScanAssembly(ScanWorkItem workItem)
        {
            const int kBufferSize = 16;

            var assembly = workItem.Assembly;

            var nsMap = new List<string>(capacity: 256);
            var buffers = new List<LinkedList<FixedSizeBuffer<Type>>>(capacity: 512);

            foreach (var type in assembly.GetTypes())
            {
                if (!IgnoreType(type))
                {
                    AddTypeToBuffer(type.Namespace, type, kBufferSize, nsMap, buffers);
                }
            }

            // Type.Namespace returns null when the type is in the global namespace.
            // It's easier to swap it out here than to deal with the null value in several other places.
            int nullNamespaceIndex = nsMap.IndexOf(null);
            if (nullNamespaceIndex > -1)
            {
                nsMap[nullNamespaceIndex] = string.Empty;
            }

            // Ensure intermediate namespaces are always included, even if
            // they don't define any types.
            for (int i = 0; i < nsMap.Count; ++i)
            {
                var namespc = nsMap[i];
                while (!string.IsNullOrEmpty(namespc))
                {
                    namespc = NamespaceUtil.GetBaseNamespace(namespc);
                    int baseNamespaceIndex = nsMap.BinarySearch(namespc, NamespaceComparer);
                    if (baseNamespaceIndex < 0)
                    {
                        int index = ~baseNamespaceIndex;
                        nsMap.Insert(index, namespc);
                        buffers.Insert(index, null);

                        if (index <= i) ++i;
                    }
                }
            }

            var typeCacheArray = new ReadOnlyArray<Type>[nsMap.Count];

            var comparison = new Comparison<Type>(TypeSorter);

            // Consolidate linked lists into contiguous buffers
            for (int i = 0; i < nsMap.Count; ++i)
            {
                var ns = nsMap[i];
                var linkedList = buffers[i];

                if (linkedList is null)
                {
                    typeCacheArray[i] = Array.Empty<Type>();
                    continue;
                }

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

                typeCacheArray[i] = contiguousBuffer;

                Array.Sort(contiguousBuffer, comparison);
            }

            workItem.SetResult(nsMap.ToArray(), typeCacheArray);
        }

        private static void ConsolidateTypesFromScannedAssembly(string[] namespaces, ReadOnlyArray<Type>[] typesInNamespaces, DistinctList<List<Type>> toSort)
        {
            var namespaceMap = _appDomainNamespaces;
            var typesMap = _typesInNamespace;

            for (int i = 0; i < namespaces.Length; ++i)
            {
                var namespc = namespaces[i];
                var typesInNamespace = typesInNamespaces[i];

                var index = namespaceMap.BinarySearch(namespc, NamespaceComparer);
                List<Type> types;
                if (index < 0)
                {
                    types = new List<Type>(capacity: typesInNamespace.Length);

                    index = ~index;
                    namespaceMap.Insert(index, namespc);
                    typesMap.Insert(index, types);
                }
                else
                {
                    types = typesMap[index];
                    toSort.Add(types);
                }

                types.AddRange(typesInNamespace.AsEnumerable());
            }
        }

        private static async Task ProcessMissingAssemblies(List<string> hintLines, TimeYielder yielder, CancellationToken token)
        {
            HashSet<Assembly> scanned;
            lock (_scanLock) { scanned = new HashSet<Assembly>(_scannedAssemblies); }

            var scanTasks = new List<Task>();
            var scanItems = new List<ScanWorkItem>();

            /* Note:
             * Ideally this code would use Unity's TypeCache API here to take advantage of its
             * performance benefits, however the API does not expose a complete collection of all
             * types in the current AppDomain. As such, we must manually enumerate all types from
             * all assemblies, and suffer the overhead.
             * Forum post:
             * https://forum.unity.com/threads/typecache-does-not-scan-all-assemblies-in-the-current-appdomain.1558037/#post-9698186
             * Issue tracker:
             * https://issuetracker.unity3d.com/issues/not-all-assemblies-are-found-in-the-current-appdomain-when-scanning-with-typecache
             */
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!scanned.Contains(assembly))
                {
                    var workItem = new ScanWorkItem(assembly);
                    var scanTask = Task.Run(() => ScanAssembly(workItem));

                    scanItems.Add(workItem);
                    scanTasks.Add(scanTask);
                }
            }

            int scanCount = scanTasks.Count;
            if (scanCount == 0) return;

            // Wait for scan tasks to complete
            while (true)
            {
                hintLines.Clear();

                int c = 0;
                for (int i = 0; i < scanItems.Count; ++i)
                {
                    char scanStatusSymbol = kCheckMark;
                    if (!scanTasks[i].IsCompleted)
                    {
                        ++c;
                        scanStatusSymbol = '-';
                    }

                    hintLines.Add($"{scanStatusSymbol} {scanItems[i].Assembly.GetName().Name}");
                }

                hintLines.Insert(0, $"Scanning assemblies ({scanCount - c}/{scanCount})");

                if (c == 0) break;
                await Task.Yield();
            }

            // All tasks are complete at this point so this will not lock the thread.
            // This call is necessary to correctly propagate exceptions and prevent
            // them going unobserved, etc.
            Task.WaitAll(scanTasks.ToArray());

            if (token.IsCancellationRequested) return;

            var toBeSorted = new DistinctList<List<Type>>(capacity: 512);
            for (int i = 0; i < scanItems.Count; ++i)
            {
                var workItem = scanItems[i];
                lock (_scanLock)
                {
                    if (_scannedAssemblies.Add(workItem.Assembly))
                    {
                        var (namespaces, typesInNamespaces) = workItem.Result;
                        ConsolidateTypesFromScannedAssembly(namespaces, typesInNamespaces, toBeSorted);
                    }
                }

                if (yielder.WantsToYield)
                {
                    hintLines.Clear();
                    hintLines.Add($"Processing assembly data ({i + 1}/{scanItems.Count})");

                    await yielder.Yield();

                    if (token.IsCancellationRequested)
                    {
                        // Though not ideal, we must ensure the type lists in the global cache
                        // are always sorted before we exit. As such, we can't return here.
                        break;
                    }
                }
            }

            if (toBeSorted.Count > 0)
            {
                var comp = new Comparison<Type>(TypeSorter);
                foreach (var list in toBeSorted)
                {
                    list.Sort(comp);
                }
            }
        }

        private async void SetupAsync(Type current, Type genericParameter)
        {
            // TODO: Temporary measure to ignore generic constraints. Need a lot more thought
            // into how to properly handle this feature.
            genericParameter = null;

            var yielder = new TimeYielder(TimeSpan.FromMilliseconds(10).Ticks);
            var token = (m_cancelSource = new()).Token;

            await ProcessMissingAssemblies(m_setupHintLines, yielder, token);

            m_setupHintLines.Clear();
            m_setupHintLines.Add("Consolidating type map");

            await yielder.YieldOptional();
            if (token.IsCancellationRequested) return;

            // Prepare to check compatibility with generic parameter
            Predicate<Type> constraintRemovePredicate = null;
            List<Type> tempCompatibleTypesList = null;
            bool checkConstraints = genericParameter is not null;
            if (checkConstraints)
            {
                constraintRemovePredicate = new Predicate<Type>(new GenericConstraintCheck(genericParameter).Incompatible);
                tempCompatibleTypesList = new List<Type>();
            }

            lock (_scanLock)
            {
                int namespaceCount = _appDomainNamespaces.Count;
                var nsList = new List<string>(capacity: namespaceCount);
                var states = new List<State>(capacity: namespaceCount);

                for (int i = 0; i < namespaceCount; ++i)
                {
                    string namespc = _appDomainNamespaces[i];

                    Type[] typesInNamespace;
                    if (checkConstraints)
                    {
                        tempCompatibleTypesList.Clear();
                        tempCompatibleTypesList.AddRange(_typesInNamespace[i]);
                        tempCompatibleTypesList.RemoveAll(constraintRemovePredicate);

                        typesInNamespace = tempCompatibleTypesList.ToArray();
                    }
                    else
                    {
                        typesInNamespace = _typesInNamespace[i].ToArray();
                    }

                    var state = new State(typesInNamespace);

                    nsList.Add(namespc);
                    states.Add(state);
                }

                var builtinTypes = new List<Type>(capacity: _builtInTypes.Length);
                foreach (var t in _builtInTypes)
                {
                    // TODO: Validate types, only grab those that match restriction.
                    builtinTypes.Add(t);
                }

                if (builtinTypes.Count > 0)
                {
                    var builtinIndex = ~nsList.BinarySearch(kBuiltInTypesIdentifier, NamespaceComparer);
                    nsList.Insert(builtinIndex, kBuiltInTypesIdentifier);
                    states.Insert(builtinIndex, new State(builtinTypes.ToArray()));
                }

                m_namespaceMap = nsList.ToArray();
                m_stateArray = states.ToArray();
            }

            m_currentSelection = current;

            m_setupComplete = true;
            m_setupHintLines.Clear();
        }

        /// <summary>
        /// Open the type selector window.
        /// </summary>
        /// <param name="controlId">
        /// ID of the control which owns the popup. This can be obtained via the
        /// 'GUIUtility.GetControlID' method.
        /// </param>
        /// <param name="current">
        /// The current value.
        /// </param>
        /// <param name="genericParameter">
        /// [Optional] The generic parameter which is to be assigned to. If assigned, the value
        /// is used to restrict the types available for selection to those which are compatible
        /// with the generic constraints declared for the parameter.
        /// <para/>
        /// See: <see cref="Type.IsGenericParameter"/>.
        /// </param>
        /// <exception cref="ArgumentException"/>
        public static void OpenTypeSelector(int controlId, Type current, Type genericParameter = null)
        {
            if (genericParameter is not null && !genericParameter.IsGenericParameter)
            {
                throw new ArgumentException(
                    "Expected a Type object that represents a generic parameter.",
                    nameof(genericParameter));
            }

            var window = ShowWindow<TypeSelector>(controlId);
            window.SetupAsync(current, genericParameter);

            // TODO: Should this auto focus and expand to show the current selection
        }
    }
}
