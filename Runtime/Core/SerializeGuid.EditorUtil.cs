#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public partial struct SerializeGuid
    {
        /// <summary>
        /// Provides convenient helper methods for working with
        /// <see cref="SerializeGuid"/> in the editor.
        /// </summary>
        public static class EditorUtil
        {
            private struct PasteArgs { public SerializeGuid guid; public SerializedProperty property; }

            internal static void GetGuidParts(
                SerializedProperty property,
                out SerializedProperty a,
                out SerializedProperty b)
            {
                a = property.FindPropertyRelative("_a");
                b = property.FindPropertyRelative("_b");
            }

            /// <summary>
            /// Get a guid value.
            /// </summary>
            /// <param name="property">A <see cref="SerializeGuid"/> property.</param>
            /// <returns>The guid value stored in the property.</returns>
            public static SerializeGuid GetGuid(SerializedProperty property)
            {
                GetGuidParts(property, out SerializedProperty a, out SerializedProperty b);

                SerializeGuid guid;
                unchecked { guid = SerializeGuid.Deserialize((ulong)a.longValue, (ulong)b.longValue); }

                return guid;
            }

            /// <summary>
            /// Set a guid value.
            /// </summary>
            /// <param name="property">A <see cref="SerializeGuid"/> property.</param>
            /// <param name="guid">The value to set.</param>
            public static void SetGuid(SerializedProperty property, SerializeGuid guid)
            {
                GetGuidParts(property, out SerializedProperty a, out SerializedProperty b);

                long la, lb;
                unchecked
                {
                    la = (long)guid.SegmentA;
                    lb = (long)guid.SegmentB;
                }

                a.longValue = la;
                b.longValue = lb;
            }

            /// <summary>
            /// Attempt to find an asset in the project with the given guid.
            /// </summary>
            /// <param name="guid">The guid to match.</param>
            /// <param name="asset">The asset, or <see langword="null"/> if none was found.</param>
            /// <returns>
            /// <see langword="true"/> if a matching asset was found; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool TryFindAsset(SerializeGuid guid, out UnityEngine.Object asset)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid.UnityGuidString);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset != null)
                        return true;
                }

                asset = null;
                return false;
            }

            /// <summary>
            /// Attempt to get the guid of an asset in the project.
            /// </summary>
            /// <param name="asset">The project asset.</param>
            /// <param name="guid">The asset's guid, or <see langword="default"/> if none was found.</param>
            /// <returns>
            /// <see langword="true"/> if the guid was obtained; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool TryGetGuidFromAsset(UnityEngine.Object asset, out SerializeGuid guid)
            {
                guid = default;
                if (asset == null)
                {
                    return false;
                }

                var path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                {
                    guid = new SerializeGuid(AssetDatabase.GUIDFromAssetPath(path).ToString());
                    return true;
                }

                return false;
            }

            private static void Copy(object o)
            {
                var guid = (SerializeGuid)o;
                GUIUtility.systemCopyBuffer = guid.UnityGuidString;
            }

            private static void Paste(object o)
            {
                var args = (PasteArgs)o;
                SetGuid(args.property, args.guid);

                args.property.serializedObject.ApplyModifiedProperties();
            }

            private static bool TryParseClipboard(out SerializeGuid guid)
            {
                var buffer = EditorGUIUtility.systemCopyBuffer;
                if (Guid.TryParse(buffer, out Guid g))
                {
                    guid = new SerializeGuid(g);
                    return true;
                }

                guid = default;
                return false;
            }

            private static void NewGuid(object o)
            {
                var prop = (SerializedProperty)o;
                SetGuid(prop, SerializeGuid.NewGuid());

                prop.serializedObject.ApplyModifiedProperties();
            }

            private static void Clear(object o)
            {
                var prop = (SerializedProperty)o;
                SetGuid(prop, default);

                prop.serializedObject.ApplyModifiedProperties();
            }

            private static void Find(object o)
            {
                var guid = (SerializeGuid)o;
                if (TryFindAsset(guid, out UnityEngine.Object asset))
                {
                    Selection.activeObject = asset;
                }
                else
                {
                    Debug.LogError($"Failed to find asset with guid {guid} in project.");
                }
            }

            public static void AddCopyGuidCommand(GenericMenu menu, SerializeGuid guid, string text = "Copy")
            {
                menu.AddItem(new GUIContent(text), false, Copy, guid);
            }

            public static void AddPasteGuidCommand(GenericMenu menu, SerializedProperty property, string text = "Paste")
            {
                var pasteFunc = TryParseClipboard(out SerializeGuid clipboardGuid)
                    ? (GenericMenu.MenuFunction2)Paste
                    : null;
                menu.AddItem(new GUIContent(text), false, pasteFunc, new PasteArgs() { guid = clipboardGuid, property = property });
            }

            public static void AddNewGuidCommand(GenericMenu menu, SerializedProperty property, string text = "New Guid")
            {
                menu.AddItem(new GUIContent(text), false, NewGuid, property);
            }

            public static void AddClearGuidCommand(GenericMenu menu, SerializedProperty property, string text = "Clear")
            {
                menu.AddItem(new GUIContent(text), false, Clear, property);
            }

            public static void AddFindAssetFromGuidCommand(GenericMenu menu, SerializeGuid guid, string text = "Find Asset with Guid")
            {
                menu.AddItem(new GUIContent(text), false, guid == default ? null : Find, guid);
            }
        }
    }
}

#endif // UNITY_EDITOR
