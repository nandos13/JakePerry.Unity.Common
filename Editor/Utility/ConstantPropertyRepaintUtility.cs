using JakePerry.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Utility class that allows constant repainting of a <see cref="PropertyDrawer"/>
    /// or <see cref="EditorWindow"/> type.
    /// </summary>
    internal static class ConstantPropertyRepaintUtility
    {
        private const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, MethodInfo> _attributeLookup = new();

        private static double _lastRepaintTime;

        private static bool TryGetPropertyDrawer(SerializedProperty property, out object propertyDrawer)
        {
            propertyDrawer = null;

            Type scriptAttributeUtilityType = ReflectionEx.GetType(typeof(Editor).Assembly, "UnityEditor.ScriptAttributeUtility");
            MethodInfo getHandlerMethod = ReflectionEx.GetMethod(scriptAttributeUtilityType, "GetHandler", kFlags, new ParamsArray<Type>(typeof(SerializedProperty)));

            object handle;
            using (ReflectionEx.RentArrayWithArgsInScope(out object[] args, property))
            {
                handle = getHandlerMethod.Invoke(null, args);
            }

            if (handle is not null)
            {
                Type propertyHandlerType = ReflectionEx.GetType(typeof(Editor).Assembly, "UnityEditor.PropertyHandler");
                PropertyInfo propertyDrawerProperty = ReflectionEx.GetProperty(propertyHandlerType, "propertyDrawer", kFlags);

                propertyDrawer = propertyDrawerProperty.GetValue(handle);
            }
            return propertyDrawer is not null;
        }

        private static bool AnyPropertyDrawerWantsRepaint(Editor editor)
        {
            SerializedObject sObj = editor.serializedObject;
            sObj.UpdateIfRequiredOrScript();

            SerializedProperty iterator = sObj.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (!TryGetPropertyDrawer(iterator, out object propertyDrawer))
                {
                    continue;
                }

                if (_attributeLookup.TryGetValue(propertyDrawer.GetType(), out MethodInfo method))
                {
                    bool wantsRepaint = true;
                    if (method is not null)
                    {
                        if (method.GetParameters().Length > 0)
                        {
                            using (ReflectionEx.RentArrayWithArgsInScope(out object[] args, sObj))
                            {
                                wantsRepaint = (bool)method.Invoke(propertyDrawer, args);
                            }
                        }
                        else
                        {
                            wantsRepaint = (bool)method.Invoke(propertyDrawer, Array.Empty<object>());
                        }
                    }

                    if (wantsRepaint) return true;
                }
            }

            return false;
        }

        private static void EditorUpdate()
        {
            const double kDelta = 0.032999999821186066;

            /* This mimics behaviour in PropertyEditor.Update, seemingly
             * to prevent performance issues from repainting too frequently.
             * Honestly I don't know the significance of the 'kDelta' value,
             * but if it works for Unity, it'll be fine here.
             */
            double time = EditorApplication.timeSinceStartup;
            if (_lastRepaintTime + kDelta >= time)
            {
                return;
            }
            _lastRepaintTime = time;

            Type genericInspectorType = ReflectionEx.GetType(typeof(Editor).Assembly, "UnityEditor.GenericInspector");

            foreach (Editor editor in ActiveEditorTracker.sharedTracker.activeEditors)
                if (genericInspectorType.IsAssignableFrom(editor.GetType()) &&
                    AnyPropertyDrawerWantsRepaint(editor))
                {
                    editor.Repaint();
                }

            EditorWindow focusWindow = EditorWindow.focusedWindow;
            if (focusWindow != null)
            {
                bool wantsRepaint = false;
                if (_attributeLookup.TryGetValue(focusWindow.GetType(), out var method))
                {
                    if (method is null)
                    {
                        wantsRepaint = true;
                    }
                    else
                    {
                        wantsRepaint = (bool)method.Invoke(focusWindow, Array.Empty<object>());
                    }
                }

                if (wantsRepaint)
                {
                    focusWindow.Repaint();
                }
            }
        }

        [InitializeOnLoadMethod]
        [DidReloadScripts]
        private static void Initialize()
        {
            const BindingFlags kMethodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;

            Dictionary<Type, MethodInfo> dict = _attributeLookup;
            dict.Clear();

            foreach (Type t in TypeCache.GetTypesWithAttribute<RequiresConstantRepaintAttribute>())
            {
                var attr = t.GetCustomAttribute<RequiresConstantRepaintAttribute>();
                string methodName = attr.If;

                MethodInfo method = null;
                if (!string.IsNullOrEmpty(methodName))
                {
                    bool isPropDrawer = typeof(PropertyDrawer).IsAssignableFrom(t);
                    if (isPropDrawer || typeof(EditorWindow).IsAssignableFrom(t))
                    {
                        method = ReflectionEx.GetMethod(t, methodName, kMethodFlags, throwOnError: false);

                        if (isPropDrawer && method is null)
                        {
                            ParamsArray<Type> types = new(typeof(SerializedObject));
                            method = ReflectionEx.GetMethod(t, methodName, kMethodFlags, types, throwOnError: false);
                        }

                        if (method is null || method.ReturnType != typeof(bool))
                        {
                            Debug.LogError(
                                "Unable to find method specified by the attribute on type " +
                                t.FullName +
                                ". Please refer to the documentation on the RequiresConstantRepaintAttribute.If " +
                                "property and check the method signature.");
                        }
                    }
                }

                dict[t] = method;
            }
        }
    }
}
