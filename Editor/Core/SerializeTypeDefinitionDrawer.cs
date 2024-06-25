using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;
using static JakePerry.Unity.TypeSerializationUtility;

namespace JakePerry.Unity
{
    // TODO: Documentation pass
    [RequiresConstantRepaint(If = nameof(IsMouseOverTotalRect))]
    [CustomPropertyDrawer(typeof(SerializeTypeDefinition))]
    public sealed class SerializeTypeDefinitionDrawer : PropertyDrawer
    {
        private const float kArgPaddingV = 4;

        private sealed class Properties
        {
            public Rect position;
            public SerializedProperty property;
            public Type type;
            public Type genericParameter;
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

        /// <summary>
        /// Indicates whether unbound generics are explicitly disallowed for the
        /// current serialized data.
        /// <para/>
        /// When this flag is set to <see langword="true"/>, generic type arguments
        /// are always shown and an unbound type definition cannot be set.
        /// </summary>
        private static bool _disallowUnboundGeneric;

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
                }
                // Note: Hover color is applied every time in case the user changes the color.
                _genericArgNameStyle.hover.textColor = SerializedTypesStyleConfig.TypeHoverColor;
                return _genericArgNameStyle;
            }
        }

        private static GUIStyle DisplayNameStyle
        {
            get
            {
                if (_displayNameStyle is null)
                {
                    _displayNameStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.UpperLeft
                    };
                    _displayNameStyle.padding.left = 0;
                    _displayNameStyle.padding.right = 0;
                }
                // Note: Hover color is applied every time in case the user changes the color.
                _displayNameStyle.hover.textColor = SerializedTypesStyleConfig.TypeHoverColor;
                return _displayNameStyle;
            }
        }

        private static GUIStyle GroupBox => _groupBox ??= new GUIStyle("GroupBox");

        private bool IsMouseOverTotalRect()
        {
            // TODO: Implement this properly so the drawer only forces repaint if the mouse
            // is over the rect of the property.
            // Remember that this PropertyDrawer may draw many properties in one editor update,
            // so we may need to cache multiple rects somewhere.
            return true;
        }

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

                var argsProp = property.FindPropertyRelative("m_genericArgs");
                if (argsProp.arraySize > 0)
                {
                    height += Spacing + LineHeight;
                }

                return height;
            }
            finally { _disallowUnboundGeneric = false; }
        }

        private static Properties BuildProperties(
            Rect position,
            SerializedProperty property,
            ref int index,
            Type genericParameter = null)
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

                var c = TempContent;
                foreach (var argType in unboundGenericArguments)
                {
                    c.text = argType.Name;
                    GenericArgumentNameStyle.CalcMinMaxWidth(c, out float nWidth, out _);

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
                        genericParameter: argType);

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
                genericParameter = genericParameter,
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

        private static void AssignType(Type type, SerializedProperty property)
        {
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
        }

        internal static bool DrawTypeSelectRect(Rect position, GUIContent content, ref Type t, Type genericParameter)
        {
            const string kHint = "SerializeTypeDefinitionDrawer.TypeSelectorButton";

            int id = GUIUtility.GetControlID(kHint.GetHashCode(), FocusType.Keyboard, position);

            var current = Event.current;
            if (current.type == EventType.ExecuteCommand &&
                StringComparer.Ordinal.Equals(current.commandName, TypeSelector.SelectionUpdatedCommand) &&
                TypeSelector.ControlID == id)
            {
                t = TypeSelector.SelectedType;
                return true;
            }

            if (EditorGUIEx.ObjectFieldButton(position, content, id))
            {
                TypeSelector.OpenTypeSelector(id, t, genericParameter);
            }

            return false;
        }

        private static void DrawTypeSelectRect(Rect position, SerializedProperty property, GUIContent content, Type t, Type genericParameter)
        {
            if (DrawTypeSelectRect(position, content, ref t, genericParameter))
            {
                AssignType(t, property);

                property.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
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

                properties.property.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }
        }

        private static void DrawGUI(Properties properties)
        {
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

                        var argName = argProperties.genericParameter.Name;
                        var c = GetTempContent(argName, argName);

                        GenericArgumentNameStyle.Draw(nameRect, c, hover, false, false, false);
                    }

                    DrawGUI(argProperties);
                }
            }

            string typeText;
            if (properties.type is null)
            {
                var serializedTypeString = properties.typeName.stringValue;

                typeText = !string.IsNullOrEmpty(serializedTypeString)
                    ? $"<Missing Type> {serializedTypeString}"
                    : "<None>";
            }
            else
            {
                typeText = properties.type.FullName;
            }

            DrawTypeSelectRect(typeRect, properties.property, GetTempContent(typeText), properties.type, properties.genericParameter);
        }

        private static NameSegmentData GetNameSegment(ref Rect rect, GUIStyle style, string text, int index)
        {
            style.CalcMinMaxWidth(GetTempContent(text), out float w, out _);

            w = Mathf.Min(w, rect.width);

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
            const string kLT = "<";
            const string kGT = ">";
            const string kComma = ", ";

            int thisIndex = index;

            // TODO: Drop generic identifier (ie. List`1).
            string text;
            if (properties.type is not null)
            {
                // TODO: Utility method for readable type names, option to use compiler aliases.
                //       Then, replace ReturnDelegatesEditorUtil.GetNiceTypeName.
                text = properties.type.Name;
            }
            else
            {
                text = index == 0 ? "Null" : properties.genericParameter.Name;
            }

            output.Add(GetNameSegment(ref rect, style, text, thisIndex));
            if (rect.width <= 0) return;

            var args = properties.genericArgProperties;
            if (args.Length > 0)
            {
                output.Add(GetNameSegment(ref rect, style, kLT, thisIndex));
                if (rect.width <= 0) return;

                foreach (var child in args)
                {
                    if (index != thisIndex)
                    {
                        output.Add(GetNameSegment(ref rect, style, kComma, thisIndex));
                        if (rect.width <= 0) return;
                    }

                    ++index;
                    CalculateNameRects(ref rect, child, style, output, index);
                    if (rect.width <= 0) return;
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

        private static void DrawTypeName(List<NameSegmentData> segments, GUIStyle style, int hoverIndex)
        {
            if (Event.current.type != EventType.Repaint) return;

            var swatch = SerializedTypesStyleConfig.TypeDisplaySwatch;
            int colorCount = swatch.Length;

            foreach (var o in segments)
            {
                var c = colorCount > 0
                    ? swatch[o.propertyIndex % colorCount]
                    : (EditorGUIUtility.isProSkin ? Color24.White : Color24.Black);

                style.normal.textColor = c;

                bool hover = o.propertyIndex == hoverIndex;

                style.Draw(o.rect, GetTempContent(o.text), hover, false, false, false);
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

                bool drawBoundGenericTypeName = properties.genericArgProperties.Length > 0;

                try
                {
                    if (drawBoundGenericTypeName)
                    {
                        var r = typeNameRect;
                        CalculateNameRects(ref r, properties, DisplayNameStyle, _nameSegments);
                    }

                    int hoverIndex = -1;
                    if (mouseOverRect)
                    {
                        if (typeNameRect.Contains(mousePos))
                        {
                            if (drawBoundGenericTypeName)
                            {
                                FindHover(_nameSegments, properties, mousePos, out hoverIndex);
                            }
                        }
                        else if (mousePos.y < typeNameRect.y)
                        {
                            FindHover(properties, mousePos, out hoverIndex);
                        }
                    }

                    DrawGUI(properties);

                    // TODO: Consider pinging the appropriate rect if we click one of these rects.
                    // Look at ObjectListArea.Frame to see how it handles pinging.
                    if (drawBoundGenericTypeName)
                    {
                        DrawTypeName(_nameSegments, DisplayNameStyle, hoverIndex);
                    }
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
