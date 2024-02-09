using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(SerializeTypeDefinition))]
    public sealed class SerializeTypeDefinitionDrawer : PropertyDrawer
    {
        private sealed class TypeSelectArgs
        {
            public SerializedProperty property;
            public Type type;
        }

        private static GenericMenu.MenuFunction2 _typeSelectCallback;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeDef = SerializeTypeDefinition.EditorUtil.GetTypeDefinition(property);
            var resolvedType = typeDef.ResolveTypeUnbound(throwOnError: false);

            // One line for main type selection content
            float height = LineHeight;

            if (resolvedType is not null && resolvedType.IsGenericTypeDefinition)
            {
                // One line for bound/unbound toggle
                height += Spacing + LineHeight;

                var wantsUnboundProp = property.FindPropertyRelative("m_wantsUnboundGeneric");
                bool drawGenericArgs = !wantsUnboundProp.boolValue;

                if (drawGenericArgs)
                {
                    var argsProp = property.FindPropertyRelative("m_genericArgs");
                    if (argsProp.arraySize > 0)
                    {
                        for (int i = 0; i < argsProp.arraySize; ++i)
                        {
                            // Dynamically add height per generic argument
                            height += Spacing;
                            height += EditorGUI.GetPropertyHeight(argsProp.GetArrayElementAtIndex(i), false);
                        }
                    }
                }
            }

            return height;
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

            if (type is not null)
            {
                int argCount = 0;
                if (type.IsGenericTypeDefinition && wantsUnboundProp.boolValue)
                {
                    // TODO: Check what happens if one argument is open, another is closed.
                    // ie. FooBase<T>
                    //     Foo<T> : FooBase<int>
                    argCount = type.GetGenericArguments().Length;
                }

                // TODO: Pump through the wrapped UnityEventTools method. Probably also extract this method out.
                typeNameProp.stringValue = type.AssemblyQualifiedName;
                argsProp.arraySize = argCount;
                wantsUnboundProp.boolValue = argCount > 0;
            }
            else
            {
                typeNameProp.stringValue = string.Empty;
                wantsUnboundProp.boolValue = false;
                argsProp.arraySize = 0;
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
                // TODO: Some times definitely dont show, ie. System.Collections.Generic is almost empty??
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

        private static GenericMenu BuildTypesMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();

            _typeSelectCallback ??= OnSelectType;

            // TODO: Add a 'None' option to clear the type

            var rootNamespace = NamespaceCache.GetGlobalNamespace();
            AddNamespaceToMenu(menu, string.Empty, rootNamespace, property);

            return menu;
        }

        private void DoGUI(Rect position, SerializedProperty property, GUIContent label, Type genericArg, int indentLevel = 0)
        {
            // TODO: Restrict selectable types by generic argument. Also look at conforming to generic restraints, etc.

            if (label != GUIContent.none)
            {
                position = EditorGUI.PrefixLabel(position, label);
            }

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

                var unboundToggleRect = position.WithHeight(LineHeight);
                position = position.PadTop(unboundToggleRect.height + Spacing);

                var wantsUnboundProp = property.FindPropertyRelative("m_wantsUnboundGeneric");
                bool drawGenericArgs = !wantsUnboundProp.boolValue;

                var unboundContent = new GUIContent(
                    text: "Bind Generic Arguments",
                    tooltip: "Should generic arguments be bound?\n" +
                    "When set to true, generic arguments can be assigned in the inspector;\n" +
                    "When false, this will resolve the generic type definition.");

                EditorGUI.BeginChangeCheck();

                bool wantsBound = !wantsUnboundProp.boolValue;
                wantsBound = EditorGUI.ToggleLeft(unboundToggleRect, unboundContent, wantsBound);

                if (EditorGUI.EndChangeCheck())
                {
                    wantsUnboundProp.boolValue = !wantsBound;

                    if (wantsBound)
                    {
                        var unboundGenericArguments = resolvedType.GetGenericArguments();
                        var argsProp = property.FindPropertyRelative("m_genericArgs");

                        argsProp.arraySize = unboundGenericArguments.Length;
                    }
                }

                if (drawGenericArgs)
                {
                    var unboundGenericArguments = resolvedType.GetGenericArguments();

                    // TODO: Validate args here in case array size is wrong, etc.
                    var argsProp = property.FindPropertyRelative("m_genericArgs");
                    if (argsProp.arraySize > 0)
                    {
                        int childIndent = indentLevel + 1;

                        for (int i = 0; i < argsProp.arraySize; ++i)
                        {
                            var genericArgProp = argsProp.GetArrayElementAtIndex(i);

                            var genericArgRect = position.WithHeight(EditorGUI.GetPropertyHeight(genericArgProp)).PadLeft(childIndent * 15f);
                            position = position.PadTop(genericArgRect.height + Spacing);

                            // TODO: Pass generic argument name (ie 'TKey') as the label. In this case, it shouldnt IndentRect
                            DoGUI(genericArgRect, genericArgProp, GUIContent.none, unboundGenericArguments[i], childIndent);
                        }
                    }
                }

                // TODO: Display read-only label that shows the closed generic type.
                //EditorGUI.LabelField(position, , EditorStyles.miniLabel);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DoGUI(position, property, label, null);
        }
    }
}
