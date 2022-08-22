using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class EditorGUIEx
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
    }
}
