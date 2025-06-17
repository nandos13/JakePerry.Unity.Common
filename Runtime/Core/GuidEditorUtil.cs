#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Provides convenient helper methods for working with the serializable
    /// <see cref="PackedGuid"/> type in the editor.
    /// </summary>
    public static class GuidEditorUtil
    {
        private record PasteArgs(PackedGuid Guid, SerializedProperty Property);

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

        private static void Copy(object o)
        {
            PackedGuid guid = (PackedGuid)o;
            GUIUtility.systemCopyBuffer = UnityHelper.GetUnityGuidString(guid);
        }

        private static void Paste(object o)
        {
            PasteArgs args = (PasteArgs)o;
            SetGuid(args.Property, args.Guid);

            args.Property.serializedObject.ApplyModifiedProperties();
        }

        private static bool TryParseClipboard(out PackedGuid guid)
        {
            string buffer = EditorGUIUtility.systemCopyBuffer;
            if (Guid.TryParse(buffer, out Guid g))
            {
                return TryGet.Pass(g, out guid);
            }

            return TryGet.Fail(out guid);
        }

        private static void NewGuid(object o)
        {
            SerializedProperty prop = (SerializedProperty)o;
            SetGuid(prop, PackedGuid.NewGuid());

            prop.serializedObject.ApplyModifiedProperties();
        }

        private static void Clear(object o)
        {
            SerializedProperty prop = (SerializedProperty)o;
            SetGuid(prop, default);

            prop.serializedObject.ApplyModifiedProperties();
        }

        private static void Find(object o)
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

        public static void AddCopyGuidCommand(GenericMenu menu, PackedGuid guid, string text = "Copy")
        {
            Enforce.Argument(menu, nameof(menu)).IsNotNull();

            menu.AddItem(new GUIContent(text), false, Copy, guid);
        }

        public static void AddPasteGuidCommand(GenericMenu menu, SerializedProperty property, string text = "Paste")
        {
            Enforce.Argument(menu, nameof(menu)).IsNotNull();
            Enforce.Argument(property, nameof(property)).IsNotNull();

            GenericMenu.MenuFunction2 pasteFunc =
                TryParseClipboard(out PackedGuid clipboardGuid) ? Paste : null;

            object state = new PasteArgs(clipboardGuid, property);
            menu.AddItem(new GUIContent(text), false, pasteFunc, state);
        }

        public static void AddNewGuidCommand(GenericMenu menu, SerializedProperty property, string text = "New Guid")
        {
            Enforce.Argument(menu, nameof(menu)).IsNotNull();
            Enforce.Argument(property, nameof(property)).IsNotNull();

            menu.AddItem(new GUIContent(text), false, NewGuid, property);
        }

        public static void AddClearGuidCommand(GenericMenu menu, SerializedProperty property, string text = "Clear")
        {
            Enforce.Argument(menu, nameof(menu)).IsNotNull();
            Enforce.Argument(property, nameof(property)).IsNotNull();

            menu.AddItem(new GUIContent(text), false, Clear, property);
        }

        public static void AddFindAssetFromGuidCommand(GenericMenu menu, PackedGuid guid, string text = "Find Asset with Guid")
        {
            Enforce.Argument(menu, nameof(menu)).IsNotNull();

            GenericMenu.MenuFunction2 func = guid.IsDefaultValue ? Find : null;

            menu.AddItem(new GUIContent(text), false, func, guid);
        }
    }
}

#endif // UNITY_EDITOR
