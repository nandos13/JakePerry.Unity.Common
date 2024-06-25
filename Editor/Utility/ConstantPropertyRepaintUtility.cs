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

            var scriptAttributeUtilityType = ReflectionEx.GetType(typeof(Editor).Assembly, "UnityEditor.ScriptAttributeUtility");
            var getHandlerMethod = ReflectionEx.GetMethod(scriptAttributeUtilityType, "GetHandler", kFlags, new ParamsArray<Type>(typeof(SerializedProperty)));

            var args = ReflectionEx.RentArrayWithArguments(property);
            var handle = getHandlerMethod.Invoke(null, args);
            ReflectionEx.ReturnArray(args);

            if (handle is not null)
            {
                var propertyHandlerType = ReflectionEx.GetType(typeof(Editor).Assembly, "UnityEditor.PropertyHandler");
                var propertyDrawerProperty = ReflectionEx.GetProperty(propertyHandlerType, "propertyDrawer", kFlags);

                propertyDrawer = propertyDrawerProperty.GetValue(handle);
            }
            return propertyDrawer is not null;
        }

        private static bool AnyPropertyDrawerWantsRepaint(Editor editor)
        {
            var sObj = editor.serializedObject;
            sObj.UpdateIfRequiredOrScript();

            var iterator = sObj.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (!TryGetPropertyDrawer(iterator, out object propertyDrawer))
                {
                    continue;
                }

                if (_attributeLookup.TryGetValue(propertyDrawer.GetType(), out var method))
                {
                    bool wantsRepaint = true;
                    if (method is not null)
                    {
                        if (method.GetParameters().Length > 0)
                        {
                            var args = ReflectionEx.RentArrayWithArguments(sObj);
                            wantsRepaint = (bool)method.Invoke(propertyDrawer, args);
                            ReflectionEx.ReturnArray(args);
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
            var time = EditorApplication.timeSinceStartup;
            if (_lastRepaintTime + kDelta >= time)
            {
                return;
            }
            _lastRepaintTime = time;

            var genericInspectorType = ReflectionEx.GetType(typeof(Editor).Assembly, "UnityEditor.GenericInspector");

            foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
                if (genericInspectorType.IsAssignableFrom(editor.GetType()) &&
                    AnyPropertyDrawerWantsRepaint(editor))
                {
                    editor.Repaint();
                }

            var focusWindow = EditorWindow.focusedWindow;
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

            var dict = _attributeLookup;
            dict.Clear();

            foreach (var t in TypeCache.GetTypesWithAttribute<RequiresConstantRepaintAttribute>())
            {
                var attr = t.GetCustomAttribute<RequiresConstantRepaintAttribute>();
                var methodName = attr.If;

                MethodInfo method = null;
                if (!string.IsNullOrEmpty(methodName))
                {
                    bool isPropDrawer = typeof(PropertyDrawer).IsAssignableFrom(t);
                    if (isPropDrawer || typeof(EditorWindow).IsAssignableFrom(t))
                    {
                        method = ReflectionEx.GetMethod(t, methodName, kMethodFlags, throwOnError: false);

                        if (isPropDrawer && method is null)
                        {
                            var types = new ParamsArray<Type>(typeof(SerializedObject));
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
