using System;
using UnityEditor;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [CustomPropertyDrawer(typeof(PackedGuid))]
    public sealed class PackedGuidDrawer : GuidDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }

        protected override void DrawGUI(PackedGuid guid, Rect position, SerializedProperty property, GUIContent label)
        {
            string unityGuidString = UnityHelper.GetUnityGuidString(guid);

            if (DrawGuidField(position, unityGuidString, out PackedGuid newGuid))
            {
                SetGuid(property, newGuid);
            }
        }
    }

    // TODO: Worth retaining the old logic with the drag-drop element?
    // This class is no longer used. Delete it later...
    internal sealed class PackedGuidDrawerOld : PropertyDrawer
    {
        private static readonly int kDragDropControlHint = "PackedGuidDragDrop".GetHashCode();

        private static GUIStyle _centeredObjectFieldStyle;

        private static UnityEngine.Object DoObjectField(Rect r, int id, PackedGuid guid)
        {
            UnityEditorHelper.TryGetProjectAsset(guid, out UnityEngine.Object asset);
            return EditorGUI.ObjectField(r, asset, typeof(UnityEngine.Object), allowSceneObjects: false);
        }

        private bool DrawDragDropTarget(Rect r, ref PackedGuid guid)
        {
            int id = GUIUtility.GetControlID(kDragDropControlHint, FocusType.Keyboard, r);

            bool isDragging = (DragAndDrop.paths?.Length ?? 0) > 0;

            Rect buttonRect = r.PadLeft(r.width - LineHeight);

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
                                return TryGet.Pass(out guid, default);
                            }
                            else
                            {
                                if (UnityEditorHelper.TryGetProjectAssetGuid(dragObj, out Guid g))
                                {
                                    return TryGet.Pass(out guid, g);
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
                                    return TryGet.Pass(out guid, default);
                                }
                                else
                                {
                                    if (UnityEditorHelper.TryGetProjectAssetGuid(selectedObj, out Guid g))
                                    {
                                        return TryGet.Pass(out guid, g);
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
    }
}
