using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(SerializeTypeDefinition))]
    public sealed class SerializeTypeDefinitionDrawer : PropertyDrawer
    {
        private const string kBuiltInsPath = "Built-in types/";

        private sealed class TypeSelectArgs
        {
            public SerializedProperty property;
            public Type type;
        }

        private static readonly Dictionary<(Type, string), bool> _allowUnboundGenericsLookup = new();

        private static GenericMenu.MenuFunction2 _typeSelectCallback;

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

        private static float GetPropertyHeight(
            SerializedProperty property,
            bool allowUnboundGenericType,
            bool drawResolvedGenericTypeName)
        {
            var typeDef = SerializeTypeDefinition.EditorUtil.GetTypeDefinition(property);
            var resolvedType = typeDef.ResolveTypeUnbound(throwOnError: false);

            // One line for main type selection content
            float height = LineHeight;

            if (resolvedType is not null && resolvedType.IsGenericTypeDefinition)
            {
                bool drawGenericArgs = true;
                if (allowUnboundGenericType)
                {
                    // One line for bound/unbound toggle
                    height += Spacing + LineHeight;

                    var wantsUnboundProp = property.FindPropertyRelative("m_wantsUnboundGeneric");
                    drawGenericArgs = !wantsUnboundProp.boolValue;
                }

                if (drawGenericArgs)
                {
                    var argsProp = property.FindPropertyRelative("m_genericArgs");
                    if (argsProp.arraySize > 0)
                    {
                        for (int i = 0; i < argsProp.arraySize; ++i)
                        {
                            // Dynamically add height per generic argument
                            height += Spacing;
                            height += GetPropertyHeight(
                                property: argsProp.GetArrayElementAtIndex(i),
                                allowUnboundGenericType: allowUnboundGenericType,
                                drawResolvedGenericTypeName: false);
                        }
                    }
                }

                if (drawResolvedGenericTypeName)
                {
                    height += Spacing + LineHeight;
                }
            }

            return height;
        }

        internal static float GetPropertyHeight(SerializedProperty property, bool allowUnboundGenericType)
        {
            return GetPropertyHeight(
                property: property,
                allowUnboundGenericType: allowUnboundGenericType,
                drawResolvedGenericTypeName: true);
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
                        argsProp.arraySize = type.GetGenericArguments().Length;
                    }
                }
                else
                {
                    wantsUnboundProp.boolValue = false;
                }

                // TODO: Pump through the wrapped UnityEventTools method. Probably also extract this method out.
                typeNameProp.stringValue = type.AssemblyQualifiedName;
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
            AddBuiltInType(menu, property, "Boolean", typeof(bool));
            AddBuiltInType(menu, property, "Byte", typeof(byte));
            AddBuiltInType(menu, property, "SByte", typeof(sbyte));
            AddBuiltInType(menu, property, "Char", typeof(char));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Floating point numbers"), false, null);

            AddBuiltInType(menu, property, "Single (float)", typeof(float));
            AddBuiltInType(menu, property, "Double", typeof(double));
            AddBuiltInType(menu, property, "Decimal", typeof(decimal));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Integers"), false, null);

            // TODO: Swap the names around. all are named via alias, these have (Int16) etc in brackets.
            AddBuiltInType(menu, property, "Int16 (short)", typeof(short));
            AddBuiltInType(menu, property, "Int32 (int)", typeof(int));
            AddBuiltInType(menu, property, "Int64 (long)", typeof(long));
            AddBuiltInType(menu, property, "UInt16 (ushort)", typeof(ushort));
            AddBuiltInType(menu, property, "UInt32 (uint)", typeof(uint));
            AddBuiltInType(menu, property, "UInt64 (ulong)", typeof(ulong));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Native-size integers"), false, null);

            AddBuiltInType(menu, property, "IntPtr (nint)", typeof(IntPtr));
            AddBuiltInType(menu, property, "UIntPtr (unint)", typeof(UIntPtr));

            menu.AddItem(new GUIContent(kBuiltInsPath + "Others"), false, null);

            AddBuiltInType(menu, property, "String", typeof(string));
            AddBuiltInType(menu, property, "Object", typeof(object));
        }

        private static GenericMenu BuildTypesMenu(SerializedProperty property)
        {
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

        private static void DrawGUI(
            Rect position,
            SerializedProperty property,
            Type genericArg,
            bool allowUnboundGenericType,
            bool drawResolvedGenericTypeName)
        {
            // TODO: Restrict selectable types by generic argument. Also look at conforming to generic restraints, etc.
            // TODO: If unbound generics are not allowed and it's currently assigned an unbound generic, show a warning/error icon.

            var typeDef = SerializeTypeDefinition.EditorUtil.GetTypeDefinition(property);
            var resolvedType = typeDef.ResolveTypeUnbound(throwOnError: false);

            GUIContent content;
            if (resolvedType is null)
            {
                content = new GUIContent("None");
            }
            else
            {
                content = new GUIContent(resolvedType.FullName);
            }

            // TODO: Address performance issues with generic menu creation.
            // This type may benefit from a new window like the object selector.

            var typeRect = position.WithHeight(LineHeight);
            position = position.PadTop(typeRect.height + Spacing);

            if (EditorGUI.DropdownButton(typeRect, content, FocusType.Passive, EditorStyles.popup))
            {
                BuildTypesMenu(property).DropDown(typeRect);
            }

            if (resolvedType is not null && resolvedType.IsGenericTypeDefinition)
            {
                // TODO: Unbound generics probably dont EVER work as a generic arg itself..
                // ie. you can have List<List<int>> but cant have a List<List<>>.

                var wantsUnboundProp = property.FindPropertyRelative("m_wantsUnboundGeneric");
                var argsProp = property.FindPropertyRelative("m_genericArgs");

                bool drawGenericArgs = true;
                if (allowUnboundGenericType)
                {
                    var unboundToggleRect = position.WithHeight(LineHeight);
                    position = position.PadTop(unboundToggleRect.height + Spacing);

                    drawGenericArgs = !wantsUnboundProp.boolValue;

                    var unboundContent = new GUIContent(
                        text: "Bind Generic Arguments",
                        tooltip: "Should generic arguments be bound?\n" +
                        "When set to true, generic arguments can be assigned in the inspector;\n" +
                        "When false, this will resolve the unbound generic type definition.");

                    EditorGUI.BeginChangeCheck();

                    bool wantsBound = !wantsUnboundProp.boolValue;
                    wantsBound = EditorGUI.ToggleLeft(unboundToggleRect, unboundContent, wantsBound);

                    if (EditorGUI.EndChangeCheck())
                    {
                        wantsUnboundProp.boolValue = !wantsBound;
                        argsProp.ClearArray();

                        if (wantsBound)
                        {
                            argsProp.arraySize = resolvedType.GetGenericArguments().Length;
                        }
                    }
                }
                // If unbound generics aren't allowed, we need to enforce it in the serialized data.
                else if (wantsUnboundProp.boolValue)
                {
                    wantsUnboundProp.boolValue = false;
                    // TODO: Error here about a previously unbound type forcing bound.
                    // Note that this is probs becuase the attribute was added after data serialized.
                    Debug.LogError("");
                }

                if (drawGenericArgs)
                {
                    var unboundGenericArguments = resolvedType.GetGenericArguments();

                    // TODO: Validate args here in case array size is wrong, etc.
                    if (argsProp.arraySize > 0)
                    {
                        for (int i = 0; i < argsProp.arraySize; ++i)
                        {
                            var genericArgProp = argsProp.GetArrayElementAtIndex(i);

                            float genericArgHeight = GetPropertyHeight(
                                property: genericArgProp,
                                allowUnboundGenericType: allowUnboundGenericType,
                                drawResolvedGenericTypeName: false);

                            var argRect = position.WithHeight(genericArgHeight);
                            position = position.PadTop(argRect.height + Spacing);

                            var argNameRect = new Rect(argRect.x, argRect.y, 40f, LineHeight);
                            argRect = argRect.PadLeft(argNameRect.width + Spacing);

                            // TODO: Improve layout for the arg name label (TKey, etc).
                            var argName = unboundGenericArguments[i].Name;
                            EditorGUI.LabelField(argNameRect, new GUIContent(argName, argName));

                            DrawGUI(
                                position: argRect,
                                property: genericArgProp,
                                genericArg: unboundGenericArguments[i],
                                allowUnboundGenericType: allowUnboundGenericType,
                                drawResolvedGenericTypeName: false);
                        }
                    }
                }

                if (drawResolvedGenericTypeName)
                {
                    var finalTypeNameRect = position.WithHeight(LineHeight);
                    //position = position.PadTop(finalTypeNameRect.height + Spacing);

                    var finalTypeName = SerializeTypeDefinition.EditorUtil.GetTypeName(typeDef);

                    EditorGUI.LabelField(finalTypeNameRect, finalTypeName, EditorStyles.miniLabel);
                }
            }
        }

        internal static void DrawGUI(Rect position, SerializedProperty property, bool allowUnboundGenericType)
        {
            DrawGUI(
                position: position,
                property: property,
                genericArg: null,
                allowUnboundGenericType: allowUnboundGenericType,
                drawResolvedGenericTypeName: true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool allowUnboundGenericType = IsUnboundGenericTypeAllowedForProperty(property);
            return GetPropertyHeight(property, allowUnboundGenericType);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // TODO: Draw a background around the rect, also provide 1-2 pixels padding between each generic arg.

            if (label != GUIContent.none)
            {
                position = EditorGUI.PrefixLabel(position, label);
            }

            bool allowUnboundGenericType = IsUnboundGenericTypeAllowedForProperty(property);
            DrawGUI(position, property, allowUnboundGenericType);
        }
    }
}
