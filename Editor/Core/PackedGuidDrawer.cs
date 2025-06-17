using System;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

// TODO: Rethink this drawer. By default, this should just display as a string.
// With some attribute, it should support drawing as if it were an object selector.
// ie. [DrawObjectGuidSelectorAttribute]
// Maybe the Resources attribute can then extend this?

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(PackedGuid))]
    public sealed class PackedGuidDrawer : PropertyDrawer
    {
        private static readonly int kDragDropControlHint = "PackedGuidDragDrop".GetHashCode();

        private static GUIStyle _centeredObjectFieldStyle;

        private bool DrawGuidField(Rect r, string guid, out PackedGuid newGuid)
        {
            EditorGUI.BeginChangeCheck();

            guid = EditorGUI.DelayedTextField(r, guid);

            if (EditorGUI.EndChangeCheck())
            {
                if (string.IsNullOrEmpty(guid))
                {
                    newGuid = default;
                    return true;
                }
                else if (Guid.TryParse(guid, out Guid g))
                {
                    newGuid = new PackedGuid(g);
                    return true;
                }

                Debug.LogError($"Failed to parse string as a valid Guid.");
            }

            newGuid = default;
            return false;
        }

        private static UnityEngine.Object DoObjectField(Rect r, int id, PackedGuid guid)
        {
            UnityEditorHelper.TryGetProjectAsset(guid, out UnityEngine.Object asset);
            return EditorGUI.ObjectField(r, asset, typeof(UnityEngine.Object), allowSceneObjects: false);
        }

        private bool DrawDragDropTarget(Rect r, ref PackedGuid guid)
        {
            int id = GUIUtility.GetControlID(kDragDropControlHint, FocusType.Keyboard, r);

            bool isDragging = (DragAndDrop.paths?.Length ?? 0) > 0;

            Rect buttonRect = new RectOffset((int)(r.width - LineHeight), 0, 0, 0).Remove(r);

            Event current = Event.current;

            switch (current.type)
            {
                case EventType.Repaint:
                    {
                        Color color = GUI.contentColor;
                        if (isDragging) GUI.contentColor = Color.yellow;

                        GUIContent content = EditorGUIUtility.IconContent("GameObject On Icon");
                        content.tooltip = "Drag & Drop a project asset to capture its GUID";

                        if (_centeredObjectFieldStyle == null)
                        {
                            _centeredObjectFieldStyle = new GUIStyle(EditorStyles.objectField);
                            _centeredObjectFieldStyle.alignment = TextAnchor.MiddleCenter;
                        }

                        _centeredObjectFieldStyle.Draw(r, content, r.Contains(current.mousePosition), false, isDragging, false);
                        GUI.contentColor = color;
                        break;
                    }

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        EditorGUI.BeginChangeCheck();
                        UnityEngine.Object dragObj = DoObjectField(r, id, guid);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (dragObj == null)
                            {
                                guid = default;
                                return true;
                            }
                            else
                            {
                                if (UnityEditorHelper.TryGetProjectAssetGuid(dragObj, out Guid g))
                                {
                                    return TryGet.Pass(g, out guid);
                                }

                                Debug.LogError($"Failed to find GUID for the dragged asset");
                            }
                        }
                        break;
                    }

                case EventType.ExecuteCommand:
                    {
                        string commandName = current.commandName;
                        if (EditorGUIEx.ObjectSelector.ObjectSelectorID == id &&
                            StringComparer.Ordinal.Equals(commandName, EditorGUIEx.ObjectSelector.ObjectSelectorUpdatedCommand))
                        {
                            current.Use();

                            EditorGUI.BeginChangeCheck();
                            UnityEngine.Object selectedObj = DoObjectField(r, id, guid);

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (selectedObj == null)
                                {
                                    guid = default;
                                    return true;
                                }
                                else
                                {
                                    if (UnityEditorHelper.TryGetProjectAssetGuid(selectedObj, out Guid g))
                                    {
                                        return TryGet.Pass(g, out guid);
                                    }

                                    Debug.LogError($"Failed to find GUID for the selected asset");
                                }
                            }
                        }

                        break;
                    }
            }

            if (EditorGUIEx.CustomGuiButton(buttonRect, id, EditorGUIEx.Styles.GetStyle("m_ObjectFieldButton"), GUIContent.none))
            {
                DoObjectField(r, id, guid);

                current.Use();
                GUIUtility.ExitGUI();
            }

            return false;
        }

        private void ShowContextMenu(PackedGuid guid, SerializedProperty property)
        {
            GenericMenu menu = new();

            GuidEditorUtil.AddCopyGuidCommand(menu, guid);
            GuidEditorUtil.AddPasteGuidCommand(menu, property);

            menu.AddSeparator(null);

            GuidEditorUtil.AddNewGuidCommand(menu, property);
            GuidEditorUtil.AddClearGuidCommand(menu, property);

            menu.AddSeparator(null);

            GuidEditorUtil.AddFindAssetFromGuidCommand(menu, guid);

            menu.ShowAsContext();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // For reasons I can't comprehend, rect height is 2 pixels larger when drawing an array element
            position.height = GetPropertyHeight(property, label);

            float dragDropWidth = LineHeight + Spacing + 36;

            position = EditorGUI.PrefixLabel(position, label);
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                PackedGuid guid = GuidEditorUtil.GetGuid(property);

                // TODO: Struct or static approach for RectOffset.
                Rect optionsRect = new RectOffset((int)(position.width - LineHeight - Spacing), 0, 0, 0).Remove(position);
                Rect guidRect = new RectOffset(0, (int)(LineHeight + dragDropWidth + Spacing * 2), 0, 0).Remove(position);
                Rect dragDropRect = new RectOffset((int)(guidRect.width + Spacing), (int)(optionsRect.width + Spacing), 0, 0).Remove(position);

                string unityGuidString = UnityHelper.GetUnityGuidString(guid);

                if (DrawGuidField(guidRect, unityGuidString, out PackedGuid newGuid))
                {
                    GuidEditorUtil.SetGuid(property, newGuid);
                }

                if (DrawDragDropTarget(dragDropRect, ref guid))
                {
                    GuidEditorUtil.SetGuid(property, guid);
                }

                // Draw options context menu button
                if (EditorGUIEx.ThreeDotMenuButton(optionsRect))
                {
                    ShowContextMenu(guid, property);
                }
            }
        }
    }
}
