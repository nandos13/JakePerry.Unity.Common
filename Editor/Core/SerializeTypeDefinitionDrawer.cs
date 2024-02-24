using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.TypeSerializationUtility;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    // TODO: Documentation pass
    [RequiresConstantRepaint]
    [CustomPropertyDrawer(typeof(SerializeTypeDefinition))]
    public sealed class SerializeTypeDefinitionDrawer : PropertyDrawer
    {
        private const string kBuiltInsPath = "Built-in types/";

        private const float kArgPaddingV = 4;

        private sealed class TypeSelectArgs
        {
            public SerializedProperty property;
            public Type type;
        }

        private sealed class Properties
        {
            public Rect position;
            public SerializedProperty property;
            public Type type;
            public Type genericArgument;
            public int index;

            public SerializedProperty typeName;
            public SerializedProperty wantsUnbound;
            public SerializedProperty genericArgs;

            public ParamsArray<Properties> genericArgProperties;
            public float genericArgNamesWidth;

            public bool hovered;
        }

        private struct NameSegmentData
        {
            public string text;
            public Rect rect;
            public int propertyIndex;
        }

        private static readonly ListPool<Properties> _propertiesPool = new();
        private static readonly List<NameSegmentData> _nameSegments = new();

        private static readonly Dictionary<(Type, string), bool> _allowUnboundGenericsLookup = new();
        private static readonly Dictionary<Type, Type[]> _unboundArgsCache = new();

        private static readonly GUIContent _tempContent = new();

        /// <summary>
        /// Indicates whether unbound generics are explicitly disallowed for the
        /// current serialized data.
        /// <para/>
        /// When this flag is set to <see langword="true"/>, generic type arguments
        /// are always shown and an unbound type definition cannot be set.
        /// </summary>
        private static bool _disallowUnboundGeneric;

        private static GenericMenu.MenuFunction2 _typeSelectCallback;

        private static GUIStyle _genericArgNameStyle;
        private static GUIStyle _displayNameStyle;
        private static GUIStyle _groupBox;

        private static GUIStyle GenericArgumentNameStyle
        {
            get
            {
                if (_genericArgNameStyle is null)
                {
                    _genericArgNameStyle = new GUIStyle(EditorStyles.miniLabel);
                    _genericArgNameStyle.hover.textColor = Color.yellow;
                }
                return _genericArgNameStyle;
            }
        }

        private static GUIStyle DisplayNameStyle
        {
            get
            {
                if (_displayNameStyle is null)
                {
                    _displayNameStyle = new GUIStyle(EditorStyles.miniLabel);
                    _displayNameStyle.alignment = TextAnchor.UpperLeft;
                    _displayNameStyle.hover.textColor = Color.yellow;
                    _displayNameStyle.padding.left = 0;
                    _displayNameStyle.padding.right = 0;
                }
                return _displayNameStyle;
            }
        }

        private static GUIStyle GroupBox => _groupBox ??= new GUIStyle("GroupBox");

        private static Type[] GetGenericArgumentsAndCache(Type t)
        {
            if (!_unboundArgsCache.TryGetValue(t, out Type[] result))
            {
                _unboundArgsCache[t] = result = t.GetGenericArguments();
            }
            return result;
        }

        private static bool IsUnboundGenericTypeAllowedForProperty(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            var path = property.propertyPath;
            var key = (targetType, path);

            if (!_allowUnboundGenericsLookup.TryGetValue(key, out bool allowUnbound))
            {
                var member = UnityEditorHelper.GetSerializedMember(property);
                var hasDisallowAttribute = member.member.GetCustomAttribute<DisallowUnboundGenericTypeAttribute>() != null;

                _allowUnboundGenericsLookup[key] = allowUnbound = !hasDisallowAttribute;
            }

            return allowUnbound;
        }

        private static void ValidateSerializedData(SerializedProperty property)
        {
            var typeNameProp = property.FindPropertyRelative("m_typeName");
            var wantsUnboundProp = property.FindPropertyRelative("m_wantsUnboundGeneric");
            var argsProp = property.FindPropertyRelative("m_genericArgs");

            var type = SerializeTypeDefinition.ResolveTypeWithCache(typeNameProp.stringValue, false, true);
            if (type?.IsGenericTypeDefinition ?? false)
            {
                var unboundGenericArguments = GetGenericArgumentsAndCache(type);

                if (wantsUnboundProp.boolValue)
                {
                    if (_disallowUnboundGeneric)
                    {
                        var errContext = property.serializedObject.targetObject;
                        Debug.LogError(
                            "SerializeTypeDefinition had unbound generic type was assigned but is not allowed. " +
                            $"Property path: {property.propertyPath}",
                            errContext);

                        wantsUnboundProp.boolValue = false;
                        argsProp.arraySize = unboundGenericArguments.Length;
                    }
                    else if (argsProp.arraySize != 0)
                    {
                        argsProp.arraySize = 0;
                    }
                }
                else
                {
                    if (argsProp.arraySize != unboundGenericArguments.Length)
                    {
                        // Clear the array first to wipe away types that may not adhere to generic type restrictions
                        argsProp.ClearArray();
                        argsProp.arraySize = unboundGenericArguments.Length;

                        var errContext = property.serializedObject.targetObject;
                        Debug.LogError(
                            $"SerializeTypeDefinition had incorrect number of generic arguments assigned. " +
                            $"Property path: {property.propertyPath}",
                            errContext);
                    }

                    for (int i = 0; i < argsProp.arraySize; ++i)
                    {
                        var arg = argsProp.GetArrayElementAtIndex(i);
                        ValidateSerializedData(arg);
                    }
                }

                // TODO: Validate each argument works with any generic restrictions (Does AssignableFrom work?)
            }
            else
            {
                wantsUnboundProp.boolValue = false;
                argsProp.ClearArray();
            }
        }

        private static float GetPropertyHeight(SerializedProperty property)
        {
            var typeNameProp = property.FindPropertyRelative("m_typeName");
            var t = SerializeTypeDefinition.ResolveTypeWithCache(typeNameProp.stringValue, false, true);

            // One line for main type selection content
            float height = LineHeight;

            if (t?.IsGenericTypeDefinition ?? false)
            {
                var wantsUnboundProp = property.FindPropertyRelative("m_wantsUnboundGeneric");
                if (!wantsUnboundProp.boolValue)
                {
                    var argsProp = property.FindPropertyRelative("m_genericArgs");
                    if (argsProp.arraySize > 0)
                    {
                        for (int i = 0; i < argsProp.arraySize; ++i)
                        {
                            height += 2;

                            // Dynamically add height per generic argument
                            height += Spacing;
                            height += GetPropertyHeight(property: argsProp.GetArrayElementAtIndex(i));
                            height += kArgPaddingV * 2;
                        }
                    }

                    height += 1;
                }
            }

            return height;
        }

        internal static float GetPropertyHeight(SerializedProperty property, bool allowUnboundGenericType)
        {
            _disallowUnboundGeneric = !allowUnboundGenericType;
            try
            {
                ValidateSerializedData(property);

                // Standard property height, with an additional line for the resolved type name.
                float height = GetPropertyHeight(property);
                height += Spacing + LineHeight;

                return height;
            }
            finally { _disallowUnboundGeneric = false; }
        }

        private static Properties BuildProperties(
            Rect position,
            SerializedProperty property,
            ref int index,
            Type genericArgument = null)
        {
            var typeName = property.FindPropertyRelative("m_typeName");
            var wantsUnbound = property.FindPropertyRelative("m_wantsUnboundGeneric");
            var genericArgs = property.FindPropertyRelative("m_genericArgs");

            var t = SerializeTypeDefinition.ResolveTypeWithCache(typeName.stringValue, false, true);

            int thisIndex = ++index;

            var genericArgProperties = ParamsArray<Properties>.Empty;

            float genericArgNamesWidth = 0f;

            bool isGenericTypeDef = t is not null && t.IsGenericTypeDefinition;
            if (isGenericTypeDef && !wantsUnbound.boolValue)
            {
                var unboundGenericArguments = GetGenericArgumentsAndCache(t);

                foreach (var argType in unboundGenericArguments)
                {
                    _tempContent.text = argType.Name;
                    GenericArgumentNameStyle.CalcMinMaxWidth(_tempContent, out float nWidth, out _);

                    genericArgNamesWidth = Mathf.Max(genericArgNamesWidth, nWidth);
                }

                var rect = position.PadTop(LineHeight + Spacing);

                using var scope = _propertiesPool.RentInScope(out var argList);

                for (int i = 0; i < genericArgs.arraySize; ++i)
                {
                    rect.y += 1;

                    var arg = genericArgs.GetArrayElementAtIndex(i);
                    var argType = unboundGenericArguments[i];

                    var argHeight = GetPropertyHeight(arg) + kArgPaddingV * 2;
                    var argRect = rect.WithHeight(argHeight);
                    rect = rect.PadTop(argHeight + Spacing + 1);

                    var argContentRect = argRect.Pad(genericArgNamesWidth + 18f, 2, kArgPaddingV, kArgPaddingV);

                    var argProperties = BuildProperties(
                        position: argContentRect,
                        property: arg,
                        index: ref index,
                        genericArgument: argType);

                    argProperties.position = argRect;

                    argList.Add(argProperties);
                }

                genericArgProperties = ParamsArray<Properties>.FromList(argList);
            }

            return new Properties()
            {
                position = position,
                property = property,
                type = t,
                genericArgument = genericArgument,
                index = thisIndex,
                typeName = typeName,
                wantsUnbound = wantsUnbound,
                genericArgs = genericArgs,
                genericArgProperties = genericArgProperties,
                genericArgNamesWidth = genericArgNamesWidth
            };
        }

        private static bool FindHover(Properties properties, Vector2 mousePos, out int index)
        {
            foreach (var child in properties.genericArgProperties)
            {
                if (FindHover(child, mousePos, out index))
                {
                    return true;
                }
            }

            if (properties.position.Contains(mousePos))
            {
                properties.hovered = true;
                index = properties.index;
                return true;
            }

            index = -1;
            return false;
        }

        private static bool IgnoreType(Type t)
        {
            if (t.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                return true;

            if (t.FullName.Contains("<PrivateImplementationDetails>"))
                return true;

            return false;
        }

        private static string GetTypeDisplayName(Type t)
        {
            if (t.IsNested)
            {
                var sb = StringBuilderCache.Acquire();

                sb.Insert(0, t.Name);

                do
                {
                    t = t.DeclaringType;

                    sb.Insert(0, '+');
                    sb.Insert(0, t.Name);
                }
                while (t.IsNested);

                return StringBuilderCache.GetStringAndRelease(ref sb);
            }

            return t.Name;
        }

        private static void OnSelectType(object e)
        {
            var args = (TypeSelectArgs)e;

            var type = args.type;
            var property = args.property;

            var typeNameProp = property.FindPropertyRelative("m_typeName");
            var wantsUnboundProp = property.FindPropertyRelative("m_wantsUnboundGeneric");
            var argsProp = property.FindPropertyRelative("m_genericArgs");

            // Always clear generic arguments
            argsProp.ClearArray();

            if (type is not null)
            {
                if (type.IsGenericTypeDefinition)
                {
                    if (!wantsUnboundProp.boolValue)
                    {
                        argsProp.arraySize = GetGenericArgumentsAndCache(type).Length;
                    }
                }
                else
                {
                    wantsUnboundProp.boolValue = false;
                }

                typeNameProp.stringValue = TidyTypeNameForSerialization(type.AssemblyQualifiedName);
            }
            else
            {
                typeNameProp.stringValue = string.Empty;
                wantsUnboundProp.boolValue = false;
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private static void AddNamespaceToMenu(
            GenericMenu menu,
            string path,
            Namespace @namespace,
            SerializedProperty property)
        {
            // Recursively add child namespaces at the top
            int nestedCount = @namespace.NestedCount;
            for (int i = 0; i < nestedCount; ++i)
            {
                var childNamespace = @namespace.GetNestedNamespace(i);

                var childName = childNamespace.Name;
                var nextPath = path.Length == 0 ? childName : $"{path}/{childName}";

                AddNamespaceToMenu(menu, nextPath, childNamespace, property);
            }

            bool didSeparator = false;
            var callback = _typeSelectCallback;

            // TODO: Check how feasible it is to alphabetically sort the types.
            // Might only be worth once this moves out to its own selector menu.
            foreach (var type in @namespace.EnumerateTypesInNamespace())
            {
                // TODO: Some items definitely dont show, ie. System.Collections.Generic is almost empty??
                if (IgnoreType(type)) continue;

                if (!didSeparator)
                {
                    menu.AddSeparator(path);
                    didSeparator = true;
                }

                var typeContent = GetTypeDisplayName(type);
                if (path.Length > 0) typeContent = $"{path}/{typeContent}";

                var args = new TypeSelectArgs() { property = property, type = type };

                menu.AddItem(new GUIContent(typeContent), false, callback, args);
            }
        }

        private static void AddBuiltInType(GenericMenu menu, SerializedProperty property, string name, Type type)
        {
            var args = new TypeSelectArgs() { property = property, type = type };
            menu.AddItem(new GUIContent(kBuiltInsPath + name), false, _typeSelectCallback, args);
        }

        private static void AddBuiltInTypes(GenericMenu menu, SerializedProperty property)
        {
            AddBuiltInType(menu, property, "bool", typeof(bool));
            AddBuiltInType(menu, property, "byte", typeof(byte));
            AddBuiltInType(menu, property, "sbyte", typeof(sbyte));
            AddBuiltInType(menu, property, "char", typeof(char));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Floating point numbers"), false, null);

            AddBuiltInType(menu, property, "float (Single)", typeof(float));
            AddBuiltInType(menu, property, "double", typeof(double));
            AddBuiltInType(menu, property, "decimal", typeof(decimal));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Integers"), false, null);

            AddBuiltInType(menu, property, "short (Int16)", typeof(short));
            AddBuiltInType(menu, property, "int (Int32)", typeof(int));
            AddBuiltInType(menu, property, "long (Int64)", typeof(long));
            AddBuiltInType(menu, property, "ushort (UInt16)", typeof(ushort));
            AddBuiltInType(menu, property, "uint (UInt32)", typeof(uint));
            AddBuiltInType(menu, property, "ulong (UInt64)", typeof(ulong));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Native-size integers"), false, null);

            AddBuiltInType(menu, property, "nint (IntPtr)", typeof(IntPtr));
            AddBuiltInType(menu, property, "unint (UIntPtr)", typeof(UIntPtr));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Others"), false, null);

            AddBuiltInType(menu, property, "string", typeof(string));
            AddBuiltInType(menu, property, "object", typeof(object));
        }

        private static GenericMenu BuildTypesMenu(SerializedProperty property)
        {
            // TODO: Editor assemblies should be ignored if the serialized data belongs to runtime?

            var menu = new GenericMenu();

            _typeSelectCallback ??= OnSelectType;

            menu.AddItem(new GUIContent("None"), false, _typeSelectCallback, null);
            menu.AddSeparator(string.Empty);

            AddBuiltInTypes(menu, property);
            menu.AddSeparator(string.Empty);

            var rootNamespace = NamespaceCache.GetGlobalNamespace();
            AddNamespaceToMenu(menu, string.Empty, rootNamespace, property);

            return menu;
        }

        private static void DrawTypeSelectRect(Rect position, SerializedProperty property, GUIContent content)
        {
            // TODO: Address performance issues with generic menu creation.
            // This type may benefit from a new window like the object selector.

            if (EditorGUI.DropdownButton(position, content, FocusType.Passive, EditorStyles.popup))
            {
                BuildTypesMenu(property).DropDown(position);
            }
        }

        private static void HandleBindingButton(Rect position, Properties properties)
        {
            bool wantsBound = !properties.wantsUnbound.boolValue;

            GUIContent unboundToggleContent;
            if (!wantsBound)
            {
                unboundToggleContent = EditorGUIUtility.IconContent("d_Unlinked@2x");
                unboundToggleContent.tooltip =
                    "Generic binding disabled.\n" +
                    "An unbound generic type will be resolved ie. typeof(List<>). Click to enable binding.";
            }
            else
            {
                unboundToggleContent = EditorGUIUtility.IconContent("d_Linked@2x");
                unboundToggleContent.tooltip =
                    "Generic binding enabled.\n" +
                    "A closed generic type will be resolved ie. typeof(List<int>). Click to disable binding.";
            }

            var btnStyle = EditorGUIEx.Styles.GetStyle("m_IconButton");
            int btnId = "SerializeTypeDefinitionDrawer.BindingToggle".GetHashCode();

            if (EditorGUIEx.CustomGuiButton(position, btnId, btnStyle, unboundToggleContent))
            {
                wantsBound = !wantsBound;

                properties.wantsUnbound.boolValue = !wantsBound;
                properties.genericArgs.ClearArray();

                if (wantsBound)
                {
                    properties.genericArgs.arraySize = GetGenericArgumentsAndCache(properties.type).Length;
                }

                GUIUtility.ExitGUI();
            }
        }

        private static void DrawGUI(Properties properties)
        {
            // TODO: Restrict selectable types by generic argument. Also look at conforming to generic restraints, etc.
            // TODO: Show a warning/error if only half bound

            var position = properties.position;

            var typeRect = position.WithHeight(LineHeight);

            if (properties.type?.IsGenericTypeDefinition ?? false)
            {
                // TODO: Unbound generics probably dont EVER work as a generic arg itself..
                // ie. you can have List<List<int>> but cant have a List<List<>>.

                if (!_disallowUnboundGeneric)
                {
                    var unboundToggleRect = typeRect.PadLeft(typeRect.width - LineHeight);
                    typeRect = typeRect.PadRight(unboundToggleRect.width + Spacing);

                    HandleBindingButton(unboundToggleRect, properties);
                }

                float nameWidth = properties.genericArgNamesWidth;

                foreach (var argProperties in properties.genericArgProperties)
                {
                    var argRect = argProperties.position;
                    var argBgRect = argRect;

                    var argLineRect = argRect.Pad(left: 4, top: 4, bottom: 4).WithWidth(1);
                    argRect = argRect.Pad(6, 2, kArgPaddingV, kArgPaddingV);

                    var nameRect = argRect.WithSize(nameWidth, LineHeight);
                    argRect = argRect.PadLeft(nameWidth + 12f);

                    argProperties.position = argRect;

                    if (Event.current.type == EventType.Repaint)
                    {
                        bool hover = argProperties.hovered;

                        GroupBox.Draw(argBgRect, GUIContent.none, -1, on: false, hover: hover);

                        if (hover)
                        {
                            var hoverColor = Color.white;
                            hoverColor.a = 0.08f;

                            var hoverBgRect = argBgRect.Pad(1, 1, 1, 1);
                            EditorGUI.DrawRect(hoverBgRect, hoverColor);
                        }

                        // TODO: Color with argument text
                        EditorGUI.DrawRect(argLineRect, Color.white);

                        var argName = argProperties.genericArgument.Name;
                        _tempContent.text = argName;
                        _tempContent.tooltip = argName;

                        GenericArgumentNameStyle.Draw(nameRect, _tempContent, hover, false, false, false);
                    }

                    DrawGUI(argProperties);
                }
            }

            _tempContent.tooltip = string.Empty;
            if (properties.type is null)
            {
                var serializedTypeString = properties.typeName.stringValue;

                _tempContent.text = !string.IsNullOrEmpty(serializedTypeString)
                    ? $"<Missing Type> {serializedTypeString}"
                    : "<None>";
            }
            else
            {
                _tempContent.text = properties.type.FullName;
            }

            DrawTypeSelectRect(typeRect, properties.property, _tempContent);
        }

        private static NameSegmentData GetNameSegment(ref Rect rect, GUIStyle style, string text, int index)
        {
            _tempContent.text = text;
            style.CalcMinMaxWidth(_tempContent, out float w, out _);

            var r = rect.WithWidth(w);
            rect = rect.PadLeft(w);

            return new()
            {
                text = text,
                rect = r,
                propertyIndex = index
            };
        }

        private static void CalculateNameRects(ref Rect rect, Properties properties, GUIStyle style, List<NameSegmentData> output, int index = 0)
        {
            // TODO: Account for masking. If we get to the end of rect, exit early.

            const string kLT = "<";
            const string kGT = ">";
            const string kComma = ", ";

            int thisIndex = index;

            string text;
            if (properties.type is not null)
            {
                text = properties.type.Name;
            }
            else
            {
                text = index == 0 ? "Null" : properties.genericArgument.Name;
            }

            output.Add(GetNameSegment(ref rect, style, text, thisIndex));

            var args = properties.genericArgProperties;
            if (args.Length > 0)
            {
                output.Add(GetNameSegment(ref rect, style, kLT, thisIndex));

                foreach (var child in args)
                {
                    if (index != thisIndex)
                    {
                        output.Add(GetNameSegment(ref rect, style, kComma, thisIndex));
                    }

                    ++index;
                    CalculateNameRects(ref rect, child, style, output, index);
                }

                output.Add(GetNameSegment(ref rect, style, kGT, thisIndex));
            }
        }

        private static bool SetHovered(Properties properties, int index, ref int i)
        {
            ++i;

            if (index == i)
            {
                properties.hovered = true;
                return true;
            }

            foreach (var child in properties.genericArgProperties)
                if (SetHovered(child, index, ref i))
                {
                    return true;
                }

            return false;
        }

        private static void FindHover(List<NameSegmentData> segments, Properties root, Vector2 mousePos, out int hoverIndex)
        {
            // Find index of hovered name segment
            hoverIndex = -1;
            foreach (var o in segments)
                if (o.rect.Contains(mousePos))
                {
                    hoverIndex = o.propertyIndex;
                    break;
                }

            if (hoverIndex > -1)
            {
                int i = -1;
                SetHovered(root, hoverIndex, ref i);
            }
        }

        // TODO: Get some better colors, rename this.
        // TODO: Perhaps also better color schemes, ie. Odd index gets blue/green,
        //       even index gets some red, pink, purple etc.
        private static readonly Color32[] _cc = new[]
        {
            new Color32(214, 157, 133, 255),
            new Color32(156, 220, 254, 255),
            new Color32(216, 160, 223, 255)
        };

        private static void DrawTypeName(List<NameSegmentData> segments, GUIStyle style, int hoverIndex)
        {
            if (Event.current.type != EventType.Repaint) return;

            _tempContent.tooltip = string.Empty;

            foreach (var o in segments)
            {
                var c = _cc[o.propertyIndex % _cc.Length];
                style.normal.textColor = c;

                bool hover = o.propertyIndex == hoverIndex;

                _tempContent.text = o.text;
                style.Draw(o.rect, _tempContent, hover, false, false, false);
            }
        }

        internal static void DrawGUI(Rect position, SerializedProperty property, bool allowUnboundGenericType)
        {
            _disallowUnboundGeneric = !allowUnboundGenericType;
            try
            {
                ValidateSerializedData(property);

                var mousePos = Event.current.mousePosition;
                bool mouseOverRect = position.Contains(mousePos);

                var typeNameRect = position.WithHeight(LineHeight, anchorBottom: true);
                position = position.PadBottom(LineHeight + Spacing);

                int i = -1;
                var properties = BuildProperties(position, property, ref i);

                try
                {
                    var nameStyle = DisplayNameStyle;

                    var r = typeNameRect;
                    CalculateNameRects(ref r, properties, nameStyle, _nameSegments);

                    int hoverIndex = -1;
                    if (mouseOverRect)
                    {
                        if (typeNameRect.Contains(mousePos))
                        {
                            FindHover(_nameSegments, properties, mousePos, out hoverIndex);
                        }
                        else if (mousePos.y < typeNameRect.y)
                        {
                            FindHover(properties, mousePos, out hoverIndex);
                        }
                    }

                    DrawGUI(properties);

                    // TODO: Consider pinging the appropriate rect if we click one of these rects.
                    // Look at ObjectListArea.Frame to see how it handles pinging.
                    DrawTypeName(_nameSegments, nameStyle, hoverIndex);
                }
                finally { _nameSegments.Clear(); }
            }
            finally { _disallowUnboundGeneric = false; }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool allowUnboundGenericType = IsUnboundGenericTypeAllowedForProperty(property);
            return GetPropertyHeight(property, allowUnboundGenericType);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (label != GUIContent.none)
            {
                position = EditorGUI.PrefixLabel(position, label);
            }

            bool allowUnboundGenericType = IsUnboundGenericTypeAllowedForProperty(property);
            DrawGUI(position, property, allowUnboundGenericType);
        }
    }
}
