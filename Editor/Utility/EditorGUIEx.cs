using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static partial class EditorGUIEx
    {
        private sealed class GuiEnabledScope : IDisposable
        {
            private static readonly Stack<bool> _restoreStates = new Stack<bool>();
            private static readonly GuiEnabledScope _inst = new GuiEnabledScope();

            public static GuiEnabledScope Push(bool enabled)
            {
                _restoreStates.Push(GUI.enabled);
                GUI.enabled = enabled;

                return _inst;
            }

            public static void Pop()
            {
                if (_restoreStates.Count > 0)
                {
                    var state = _restoreStates.Pop();
                    GUI.enabled = state;
                }
                else
                {
                    throw new InvalidOperationException("Push/pop mismatch.");
                }
            }

            public void Dispose() => Pop();
        }

        public static IDisposable DisabledBlock => GuiEnabledScope.Push(false);

        public static IDisposable EnabledBlock => GuiEnabledScope.Push(true);

        public static IDisposable GetDisabledBlock(bool disabled)
        {
            return disabled ? DisabledBlock : EnabledBlock;
        }

        private static MonoScript GetMonoScript(UnityEngine.Object target)
        {
            if (target is MonoBehaviour mb)
                return MonoScript.FromMonoBehaviour(mb);

            return MonoScript.FromScriptableObject((ScriptableObject)target);
        }

        public static void DrawMonoScriptField(Rect rect, UnityEngine.Object target)
        {
            using (DisabledBlock)
                EditorGUI.ObjectField(rect, "Script", GetMonoScript(target), typeof(MonoScript), false);
        }

        public static void DrawMonoScriptField(UnityEngine.Object target)
        {
            using (DisabledBlock)
                EditorGUILayout.ObjectField("Script", GetMonoScript(target), typeof(MonoScript), false);
        }

        public static void DrawRectOutline(Rect rect, Color color, float weight = 1f)
        {
            weight = Mathf.Max(weight, 1f);

            // Top
            var horizontalLineRect = new Rect(rect.x, rect.y, rect.width, weight);
            EditorGUI.DrawRect(horizontalLineRect, color);

            // Bottom
            horizontalLineRect.y += rect.height - weight;
            EditorGUI.DrawRect(horizontalLineRect, color);

            // Left
            var verticalLineRect = new Rect(rect.x, rect.y, weight, rect.height);
            EditorGUI.DrawRect(verticalLineRect, color);

            // Right
            verticalLineRect.x += rect.width - weight;
            EditorGUI.DrawRect(verticalLineRect, color);
        }

        /// <summary>
        /// Begin a masked area inside your GUI.
        /// </summary>
        /// <param name="viewportRect">
        /// The actual rect occupied on the GUI.
        /// </param>
        /// <param name="contentRect">
        /// The total rect of all content inside the masked area.
        /// </param>
        public static void BeginMaskedArea(Rect viewportRect, Rect contentRect)
        {
            // Small hack: We take advantage of the masking capabilities of the scroll view,
            // without actually using the scrolling functionality or rendering the scroll bars.
            var styleNone = GUIStyle.none;
            GUI.BeginScrollView(viewportRect, default, contentRect, styleNone, styleNone);
        }

        /// <summary>
        /// Ends a masked area started with a call to <see cref="BeginMaskedArea(Rect, Rect)"/>
        /// </summary>
        public static void EndMaskedArea()
        {
            GUI.EndScrollView();
        }

        /// <summary>
        /// Scope for managing a masked area in the GUI.
        /// </summary>
        public sealed class MaskedAreaScope : GUI.Scope
        {
            public MaskedAreaScope(Rect viewportRect, Rect contentRect)
            {
                BeginMaskedArea(viewportRect, contentRect);
            }

            protected override void CloseScope()
            {
                EndMaskedArea();
            }
        }

        public static bool CustomGuiButton(Rect rect, int id, GUIStyle style, GUIContent content)
        {
            bool result = false;

            var evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                rect = style.margin.Remove(rect);

                bool active = DragAndDrop.activeControlID == id;
                bool hover = rect.Contains(Event.current.mousePosition);

                style.Draw(rect, content, id, active, hover);
            }
            else if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (GUI.enabled && rect.Contains(evt.mousePosition))
                {
                    result = true;
                    evt.Use();
                }
            }

            return result;
        }

        public static int CancellableTextField(Rect rect, ref string text, bool cancelOnClickAway = true)
        {
            int code = 0;

            if (GUI.enabled)
            {
                var evt = Event.current;
                if (evt.type == EventType.KeyDown)
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        code = 1;
                    }
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        code = -1;
                        evt.Use();
                    }
                }

                if (cancelOnClickAway)
                {
                    if ((evt.type == EventType.MouseDown || evt.type == EventType.TouchDown) &&
                        !rect.Contains(evt.mousePosition))
                    {
                        code = -1;
                        evt.Use();
                    }
                }
            }

            text = EditorGUI.TextField(rect, text);

            if (code == -1)
                text = null;

            return code;
        }

        public static bool ThreeDotMenuButton(Rect position)
        {
            bool result = false;
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                var id = GUIUtility.GetControlID(kOptionsControlHint, FocusType.Keyboard, position);

                var icon = EditorGUIUtility.IconContent("_Menu@2x");
                if (CustomGuiButton(position, id, GetStyle("m_IconButton"), icon))
                {
                    result = true;
                }
            }

            return result;
        }
    }
}
