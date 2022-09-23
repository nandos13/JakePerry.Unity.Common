using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(SerializeGuid))]
    public sealed class SerializeGuidDrawer : PropertyDrawer
    {
        private const float kElementSpacing = 2f;
        private const float kOptionsButtonSize = 28;

        private readonly Dictionary<string, PropertyDrawerState> m_stateLookup = new Dictionary<string, PropertyDrawerState>();

        private float GetOptionsHeight(PropertyDrawerState state) => EditorGUIUtility.singleLineHeight * state.ShowOptions.faded;

        private PropertyDrawerState GetState(SerializedProperty property)
        {
            var propertyPath = property.propertyPath;
            if (!m_stateLookup.TryGetValue(propertyPath, out PropertyDrawerState state))
            {
                state = PropertyDrawerState.Get(property);
                m_stateLookup[propertyPath] = state;
            }

            return state;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var layout = new EditorLayoutHelper(0, 0, 0);
            var state = GetState(property);

            // Main content rect
            layout.SimulateRect();

            // Options content rect
            if (state.ShowOptions.value)
            {
                layout.SimulateRect(GetOptionsHeight(state));
            }

            return layout.TotalHeight;
        }

        private bool DrawGuidField(Rect r, GUIContent label, string guid, out Guid newGuid)
        {
            var changeScope = new EditorGUI.ChangeCheckScope();
            using (changeScope)
            {
                guid = EditorGUI.DelayedTextField(r, label, guid);
            }

            if (changeScope.changed)
            {
                if (Guid.TryParse(guid, out newGuid))
                {
                    return true;
                }

                Debug.LogError($"Failed to parse string as a valid Guid.");
            }

            newGuid = default;
            return false;
        }

        private void DrawOptionsToggleButton(Rect r, PropertyDrawerState state)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                var icon = EditorGUIUtility.IconContent("d_SceneViewTools@2x");
                if (GUI.Button(r, icon))
                {
                    state.ShowOptions.target = !state.ShowOptions.target;
                }
            }
        }

        private void DrawNewGuidButton(Rect r, SerializedProperty a, SerializedProperty b)
        {
            var newGuidContent = EditorGUIUtility.IconContent("P4_Updating@2x");
            newGuidContent.tooltip = "Create a new random Guid.";

            if (GUI.Button(r, newGuidContent))
            {
                var newGuid = SerializeGuid.NewGuid();

                unchecked { a.longValue = (long)newGuid.SegmentA; }
                unchecked { b.longValue = (long)newGuid.SegmentB; }
            }
        }

        private void DrawClearGuidButton(Rect r, SerializedProperty a, SerializedProperty b)
        {
            var newGuidContent = EditorGUIUtility.IconContent("d_Grid.EraserTool@2x");
            newGuidContent.tooltip = "Clear the Guid.";

            if (GUI.Button(r, newGuidContent))
            {
                var newGuid = (SerializeGuid)default;

                unchecked { a.longValue = (long)newGuid.SegmentA; }
                unchecked { b.longValue = (long)newGuid.SegmentB; }
            }
        }

        private void DrawCopyToClipboardButton(Rect r, SerializeGuid guid)
        {
            var clipboardContent = EditorGUIUtility.IconContent("Clipboard");
            clipboardContent.tooltip = "Copy the current Guid to the clipboard.";

            if (GUI.Button(r, clipboardContent))
            {
                GUIUtility.systemCopyBuffer = guid.UnityGuidString;
            }
        }

        private void DrawSearchButton(Rect r, SerializeGuid guid)
        {
            var searchContent = EditorGUIUtility.IconContent("d_Search Icon");
            searchContent.tooltip = "Search for asset with this Guid.";

            if (GUI.Button(r, searchContent))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid.UnityGuidString);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset != null)
                    {
                        Selection.activeObject = asset;
                    }
                    else
                    {
                        Debug.LogError($"Failed to load asset from the database at path {assetPath}.");
                    }
                }
                else
                {
                    Debug.Log($"No asset found in the database with guid {guid.UnityGuidString}.");
                }
            }
        }

        private void DrawMaskedOptionsArea(Rect r, SerializeGuid guid, SerializedProperty a, SerializedProperty b, PropertyDrawerState state)
        {
            var optionsRect = r;
            optionsRect = optionsRect.PadLeft(EditorGUIUtility.labelWidth + kElementSpacing);

            using (new EditorGUIEx.MaskedAreaScope(r.WithHeight(GetOptionsHeight(state)), r))
            {
                using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
                {
                    const int kButtonCount = 4;
#if false // Code for clamping button size, if I decide to restore this logic in future
                    const float kMaxButtonAspect = 2f;

                    float minButtonWidth = optionsRect.height;
                    float maxButtonWidth = minButtonWidth * kMaxButtonAspect;
                    float totalSpacing = kElementSpacing * (kButtonCount - 1);

                    float minTotalSize = minButtonWidth * kButtonCount + totalSpacing;
                    float maxTotalSize = maxButtonWidth * kButtonCount + totalSpacing;

                    float totalSize = Mathf.Clamp(optionsRect.width, minTotalSize, maxTotalSize);
                    optionsRect = optionsRect.PadLeft(optionsRect.width - totalSize);
#endif

                    var list = ListPool<Rect>.Get();
                    RectEx.SliceX(optionsRect, kButtonCount, list, kElementSpacing);

                    DrawNewGuidButton(list[0], a, b);
                    DrawClearGuidButton(list[1], a, b);

                    DrawCopyToClipboardButton(list[2], guid);
                    DrawSearchButton(list[3], guid);

                    ListPool<Rect>.Release(ref list);
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (label is null || label == GUIContent.none)
                label = new GUIContent("Guid");

            var layout = new EditorLayoutHelper(position);
            var state = GetState(property);

            var a = property.FindPropertyRelative("_a");
            var b = property.FindPropertyRelative("_b");

            SerializeGuid guid;
            unchecked { guid = SerializeGuid.Deserialize((ulong)a.longValue, (ulong)b.longValue); }

            var guidRect = layout.GetRect();
            var optionsButtonRect = guidRect;

            optionsButtonRect = optionsButtonRect.PadLeft(optionsButtonRect.width - kOptionsButtonSize);
            guidRect = guidRect.PadRight(kElementSpacing + optionsButtonRect.width);

            if (DrawGuidField(guidRect, label, guid.UnityGuidString, out Guid newGuid))
            {
                guid = new SerializeGuid(newGuid);
                unchecked { a.longValue = (long)guid.SegmentA; b.longValue = (long)guid.SegmentB; }
            }

            DrawOptionsToggleButton(optionsButtonRect, state);

            if (state.ShowOptions.value)
            {
                DrawMaskedOptionsArea(layout.GetRect(), guid, a, b, state);
            }
        }

        private readonly struct PropertyDrawerState
        {
            private static readonly string kTypeName = typeof(SerializeGuidDrawer).FullName;
            private static string GetPrefsKey_ShowOptions(string guid) => $"{kTypeName}.ShowOptions.{guid}";

            private readonly AnimBool m_showOptions;

            public AnimBool ShowOptions => m_showOptions;

            private PropertyDrawerState(string prefsGuid)
            {
                var showOptionsKey = GetPrefsKey_ShowOptions(prefsGuid);
                var showOptionsCapture = new AnimBool(EditorPrefs.GetBool(showOptionsKey, false));

                m_showOptions = showOptionsCapture;

                showOptionsCapture.valueChanged.AddListener(() =>
                {
                    if (showOptionsCapture != null)
                        EditorPrefs.SetBool(showOptionsKey, showOptionsCapture.target);
                });
            }

            private static string GenerateGuidForEditorPrefs(string propertyPath, Type targetType)
            {
                var lastSplitIndex = propertyPath.LastIndexOf('.');

                int segmentCount = 1;
                for (int i = 0; i < propertyPath.Length; i++)
                    if (propertyPath[i] == '.')
                        ++segmentCount;

                ulong ul1, ul2;
                unchecked
                {
                    ulong h1 = (uint)StringComparer.Ordinal.GetHashCode(targetType.FullName);
                    ulong h2 = (uint)((29 * 31 + propertyPath.Length) * 31 + segmentCount);

                    ulong h3 = (uint)StringComparer.Ordinal.GetHashCode(propertyPath);
                    ulong h4 = (uint)(lastSplitIndex > -1 ? StringComparer.Ordinal.GetHashCode(propertyPath.Substring(lastSplitIndex + 1)) : 0);

                    ul1 = ((h1 << 32) | h2);
                    ul2 = ((h3 << 32) | h4);
                }

                return new SerializeGuid(ul1, ul2).UnityGuidString;
            }

            public static PropertyDrawerState Get(SerializedProperty property)
            {
                var propertyPath = property.propertyPath;
                var obj = property.serializedObject.targetObject;

                var prefsGuid = GenerateGuidForEditorPrefs(propertyPath, obj.GetType());

                return new PropertyDrawerState(prefsGuid);
            }
        }
    }
}
