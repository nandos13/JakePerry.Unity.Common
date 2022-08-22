using System;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class EditorGUIEx
    {
        private sealed class GuiEnabledScope : IDisposable
        {
            // Flag that remembers the enable state when this scope is created
            private readonly bool m_wasEnabled;

            public GuiEnabledScope(bool enabled) { m_wasEnabled = GUI.enabled; GUI.enabled = enabled; }

            public void Dispose() { GUI.enabled = m_wasEnabled; }
        }

        public static IDisposable DisabledBlock => new GuiEnabledScope(false);

        public static IDisposable EnabledBlock => new GuiEnabledScope(true);

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
