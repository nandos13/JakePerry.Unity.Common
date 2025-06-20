using System;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    /// <summary>
    /// Base class for property drawers that draw a <see cref="PackedGuid"/>
    /// field in the inspector.
    /// </summary>
    public abstract class GuidDrawer : PropertyDrawer
    {
        private record PasteArgs(PackedGuid Guid, SerializedProperty Property);

        protected virtual bool DrawPrefixLabel => true;

        protected virtual bool DrawContextMenuButton => true;

        /// <summary>
        /// Get a guid value.
        /// </summary>
        /// <param name="property">
        /// A <see cref="PackedGuid"/> property.
        /// </param>
        /// <returns>
        /// The guid value stored in the property.
        /// </returns>
        public static PackedGuid GetGuid(SerializedProperty property)
        {
            Enforce.Argument(property, nameof(property)).IsNotNull();

            SerializedProperty a = property.FindPropertyRelative("a");
            SerializedProperty b = property.FindPropertyRelative("b");

            return new PackedGuid(a.ulongValue, b.ulongValue);
        }

        /// <summary>
        /// Set a guid value.
        /// </summary>
        /// <param name="property">
        /// A <see cref="PackedGuid"/> property.
        /// </param>
        /// <param name="guid">
        /// The value to set.
        /// </param>
        public static void SetGuid(SerializedProperty property, PackedGuid guid)
        {
            Enforce.Argument(property, nameof(property)).IsNotNull();

            SerializedProperty a = property.FindPropertyRelative("a");
            SerializedProperty b = property.FindPropertyRelative("b");

            a.ulongValue = guid.a;
            b.ulongValue = guid.b;
        }

        protected static void AddCopyGuidCommand(GenericMenu menu, PackedGuid guid, string text = "Copy")
        {
            static void Copy(object o)
            {
                PackedGuid guid = (PackedGuid)o;
                GUIUtility.systemCopyBuffer = UnityHelper.GetUnityGuidString(guid);
            }

            Enforce.Argument(menu, nameof(menu)).IsNotNull();

            menu.AddItem(new GUIContent(text), false, Copy, guid);
        }

        protected static void AddPasteGuidCommand(GenericMenu menu, SerializedProperty property, string text = "Paste")
        {
            static void Paste(object o)
            {
                PasteArgs args = (PasteArgs)o;
                SetGuid(args.Property, args.Guid);

                args.Property.serializedObject.ApplyModifiedProperties();
            }

            static bool TryParseClipboard(out PackedGuid guid)
            {
                string buffer = EditorGUIUtility.systemCopyBuffer;
                if (Guid.TryParse(buffer, out Guid g))
                {
                    return TryGet.Pass(out guid, g);
                }

                return TryGet.Fail(out guid);
            }

            Enforce.Argument(menu, nameof(menu)).IsNotNull();
            Enforce.Argument(property, nameof(property)).IsNotNull();

            GenericMenu.MenuFunction2 pasteFunc =
                TryParseClipboard(out PackedGuid clipboardGuid) ? Paste : null;

            object state = new PasteArgs(clipboardGuid, property);
            menu.AddItem(new GUIContent(text), false, pasteFunc, state);
        }

        protected static void AddNewGuidCommand(GenericMenu menu, SerializedProperty property, string text = "New Guid")
        {
            static void NewGuid(object o)
            {
                SerializedProperty prop = (SerializedProperty)o;
                SetGuid(prop, PackedGuid.NewGuid());

                prop.serializedObject.ApplyModifiedProperties();
            }

            Enforce.Argument(menu, nameof(menu)).IsNotNull();
            Enforce.Argument(property, nameof(property)).IsNotNull();

            menu.AddItem(new GUIContent(text), false, NewGuid, property);
        }

        protected static void AddClearGuidCommand(GenericMenu menu, SerializedProperty property, string text = "Clear")
        {
            static void Clear(object o)
            {
                SerializedProperty prop = (SerializedProperty)o;
                SetGuid(prop, default);

                prop.serializedObject.ApplyModifiedProperties();
            }

            Enforce.Argument(menu, nameof(menu)).IsNotNull();
            Enforce.Argument(property, nameof(property)).IsNotNull();

            menu.AddItem(new GUIContent(text), false, Clear, property);
        }

        protected static void AddFindAssetFromGuidCommand(GenericMenu menu, PackedGuid guid, string text = "Find Asset with Guid")
        {
            static void Find(object o)
            {
                PackedGuid guid = (PackedGuid)o;
                if (UnityEditorHelper.TryGetProjectAsset(guid, out UnityEngine.Object asset))
                {
                    Selection.activeObject = asset;
                }
                else
                {
                    Debug.LogError($"Failed to find asset with guid {guid} in project.");
                }
            }

            Enforce.Argument(menu, nameof(menu)).IsNotNull();

            GenericMenu.MenuFunction2 func = guid.IsDefaultValue ? Find : null;

            menu.AddItem(new GUIContent(text), false, func, guid);
        }

        protected virtual GenericMenu ConstructContextMenu(PackedGuid guid, SerializedProperty property)
        {
            GenericMenu menu = new();

            // TODO: Move these to this class as protected? Probably makes more sense, if the resources and object one are gonna derive this class.
            AddCopyGuidCommand(menu, guid);
            AddPasteGuidCommand(menu, property);

            menu.AddSeparator(null);

            AddNewGuidCommand(menu, property);
            AddClearGuidCommand(menu, property);

            return menu;
        }

        protected bool DrawGuidField(Rect r, string guid, out PackedGuid newGuid)
        {
            EditorGUI.BeginChangeCheck();

            guid = EditorGUI.DelayedTextField(r, guid);

            if (EditorGUI.EndChangeCheck())
            {
                if (string.IsNullOrEmpty(guid))
                {
                    return TryGet.Pass(out newGuid, default);
                }
                else if (Guid.TryParse(guid, out Guid g))
                {
                    return TryGet.Pass(out newGuid, new PackedGuid(g));
                }

                Debug.LogError($"Failed to parse string as a valid Guid.");
            }

            return TryGet.Fail(out newGuid);
        }

        public override abstract float GetPropertyHeight(SerializedProperty property, GUIContent label);

        protected abstract void DrawGUI(PackedGuid guid, Rect position, SerializedProperty property, GUIContent label);

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (DrawPrefixLabel)
            {
                position = EditorGUI.PrefixLabel(position, label);
            }

            PackedGuid guid = GetGuid(property);

            using (EditorGUIEx.IndentLevelScope.Zero)
            {
                Rect optionsRect = default;
                if (DrawContextMenuButton)
                {
                    float optionsBtnSize = Mathf.Min(LineHeight, position.height);

                    optionsRect = position.WithWidth(optionsBtnSize, anchorRight: true);
                    position = position.PadRight(optionsBtnSize + Spacing * 2);
                }

                DrawGUI(guid, position, property, label);

                // Draw options context menu button
                if (DrawContextMenuButton && EditorGUIEx.ThreeDotMenuButton(optionsRect))
                {
                    ConstructContextMenu(guid, property).ShowAsContext();
                }
            }
        }
    }
}
