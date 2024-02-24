using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;

namespace JakePerry.Unity
{
    /// <summary>
    /// Utility class that allows constant repainting of a <see cref="PropertyDrawer"/> type.
    /// </summary>
    internal static class ConstantPropertyRepaintUtility
    {
        private const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        private static Type GenericInspectorType =>
            UnityInternalsHelper.GetType(typeof(Editor).Assembly, "UnityEditor.GenericInspector");

        private static Type ScriptAttributeUtilityType =>
            UnityInternalsHelper.GetType(typeof(Editor).Assembly, "UnityEditor.ScriptAttributeUtility");

        private static Type PropertyHandlerType =>
            UnityInternalsHelper.GetType(typeof(Editor).Assembly, "UnityEditor.PropertyHandler");

        private static MethodInfo GetHandlerMethod =>
            UnityInternalsHelper.GetMethod(ScriptAttributeUtilityType, "GetHandler", kFlags, new ParamsArray<Type>(typeof(SerializedProperty)));

        private static PropertyInfo PropertyDrawerProperty =>
            UnityInternalsHelper.GetProperty(PropertyHandlerType, "propertyDrawer", kFlags);

        private static TypeCache.TypeCollection _attributedTypes;

        private static double _lastRepaintTime;

        private static void EditorUpdate()
        {
            const double kDelta = 0.032999999821186066;

            // This mimics behaviour in PropertyEditor.Update, seemingly
            // to prevent performance issues from repainting too frequently.
            // Honestly I don't know the significance of the 'kDelta' value,
            // but if it works for Unity, it'll be fine here.
            var time = EditorApplication.timeSinceStartup;
            if (_lastRepaintTime + kDelta >= time)
            {
                return;
            }
            _lastRepaintTime = time;

            var args = ReflectionEx.RentArray(1);

            foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
            {
                if (GenericInspectorType.IsAssignableFrom(editor.GetType()))
                {
                    var sObj = editor.serializedObject;
                    sObj.UpdateIfRequiredOrScript();

                    var iterator = sObj.GetIterator();
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        enterChildren = false;

                        args[0] = iterator;

                        var handle = GetHandlerMethod.Invoke(null, args);
                        if (handle is null) continue;

                        var propertyDrawer = PropertyDrawerProperty.GetValue(handle);
                        if (propertyDrawer is null) continue;

                        if (_attributedTypes.Contains(propertyDrawer.GetType()))
                        {
                            editor.Repaint();
                        }
                    }
                }
            }

            ReflectionEx.ReturnArray(args);
        }

        [InitializeOnLoadMethod]
        [DidReloadScripts]
        private static void Initialize()
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;

            _attributedTypes = TypeCache.GetTypesWithAttribute<RequiresConstantRepaintAttribute>();
        }
    }
}
