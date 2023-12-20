using System;
using UnityEditor;
using UnityEngine;
using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(SerializeGuid))]
    public sealed class SerializeGuidDrawer : PropertyDrawer
    {
        private static readonly int kDragDropControlHint = "SerializeGuidDragDrop".GetHashCode();

        private static GUIStyle _centeredObjectFieldStyle;

        private bool DrawGuidField(Rect r, string guid, out SerializeGuid newGuid)
        {
            EditorGUI.BeginChangeCheck();

            // TODO: Change from a text field to regular label, allow it to be set via context menu?
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
                    newGuid = new SerializeGuid(g);
                    return true;
                }

                Debug.LogError($"Failed to parse string as a valid Guid.");
            }

            newGuid = default;
            return false;
        }

        private static UnityEngine.Object DoObjectField(Rect r, int id, SerializeGuid guid)
        {
            SerializeGuid.EditorUtil.TryFindAsset(guid, out UnityEngine.Object asset);
            return EditorGUI.ObjectField(r, asset, typeof(UnityEngine.Object), allowSceneObjects: false);
        }

        private bool DrawDragDropTarget(Rect r, ref SerializeGuid guid)
        {
            var id = GUIUtility.GetControlID(kDragDropControlHint, FocusType.Keyboard, r);

            bool isDragging = (DragAndDrop.paths?.Length ?? 0) > 0;

            var buttonRect = new RectOffset((int)(r.width - LineHeight), 0, 0, 0).Remove(r);

            var current = Event.current;

            switch (current.type)
            {
                case EventType.Repaint:
                    {
                        var color = GUI.contentColor;
                        if (isDragging) GUI.contentColor = Color.yellow;

                        var content = EditorGUIUtility.IconContent("GameObject On Icon");
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
                        var dragObj = DoObjectField(r, id, guid);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (dragObj == null)
                            {
                                guid = default;
                                return true;
                            }
                            else
                            {
                                if (SerializeGuid.EditorUtil.TryGetGuidFromAsset(dragObj, out SerializeGuid g))
                                {
                                    guid = g;
                                    return true;
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
                            var selectedObj = DoObjectField(r, id, guid);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (selectedObj == null)
                                {
                                    guid = default;
                                    return true;
                                }
                                else
                                {
                                    if (SerializeGuid.EditorUtil.TryGetGuidFromAsset(selectedObj, out SerializeGuid g))
                                    {
                                        guid = g;
                                        return true;
                                    }

                                    Debug.LogError($"Failed to find GUID for the selected asset");
                                }
                            }
                        }

                        break;
                    }
            }

            if (EditorGUIEx.CustomGuiButton(buttonRect, id, EditorGUIEx.GetStyle("m_ObjectFieldButton"), GUIContent.none))
            {
                DoObjectField(r, id, guid);

                current.Use();
                GUIUtility.ExitGUI();
            }

            return false;
        }

        private void ShowContextMenu(SerializeGuid guid, SerializedProperty property)
        {
            var menu = new GenericMenu();

            SerializeGuid.EditorUtil.AddCopyGuidCommand(menu, guid);
            SerializeGuid.EditorUtil.AddPasteGuidCommand(menu, property);

            menu.AddSeparator(null);

            SerializeGuid.EditorUtil.AddNewGuidCommand(menu, property);
            SerializeGuid.EditorUtil.AddClearGuidCommand(menu, property);

            menu.AddSeparator(null);

            SerializeGuid.EditorUtil.AddFindAssetFromGuidCommand(menu, guid);

            menu.ShowAsContext();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float dragDropWidth = LineHeight + Spacing + 36;

            position = EditorGUI.PrefixLabel(position, label);

            var guid = SerializeGuid.EditorUtil.GetGuid(property);

            var optionsRect = new RectOffset((int)(position.width - LineHeight - Spacing), 0, 0, 0).Remove(position);
            var guidRect = new RectOffset(0, (int)(LineHeight + dragDropWidth + Spacing * 2), 0, 0).Remove(position);
            var dragDropRect = new RectOffset((int)(guidRect.width + Spacing), (int)(optionsRect.width + Spacing), 0, 0).Remove(position);

            if (DrawGuidField(guidRect, guid.UnityGuidString, out SerializeGuid newGuid))
            {
                SerializeGuid.EditorUtil.SetGuid(property, newGuid);
            }
            if (DrawDragDropTarget(dragDropRect, ref guid))
            {
                SerializeGuid.EditorUtil.SetGuid(property, guid);
            }

            // Draw options context menu button
            if (EditorGUIEx.ThreeDotMenuButton(optionsRect))
            {
                ShowContextMenu(guid, property);
            }
        }
    }
}
