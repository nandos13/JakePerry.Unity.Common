using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static partial class EditorGUIEx
    {
        /// <summary>
        /// Exposes some elements of the internal UnityEditor.ObjectSelector class which is used
        /// to pick objects in the inspector.
        /// </summary>
        public static class ObjectSelector
        {
            private static Type _objectSelectorType;
            private static PropertyInfo _objectSelectorGetProperty;
            private static FieldInfo _objectSelectorIDProperty;

            private static string _objSelCmdClosed;
            private static string _objSelCmdUpdated;
            private static string _objSelCmdCanceled;
            private static string _objSelCmdSelectionDone;

            private static Type ObjectSelectorType
                => (_objectSelectorType ??= typeof(EditorWindow).Assembly.GetType("UnityEditor.ObjectSelector"));

            private static object ObjectSelectorInst
                => (_objectSelectorGetProperty ??= ObjectSelectorType.GetProperty("get", (BindingFlags)0x18)).GetValue(null);

            /// <summary>
            /// Exposes the command name used when the ObjectSelector is closed.
            /// </summary>
            /// <seealso cref="Event.commandName"/>
            public static string ObjectSelectorClosedCommand
                => (_objSelCmdClosed ??= (string)ObjectSelectorType.GetField("ObjectSelectorClosedCommand", (BindingFlags)0x18).GetValue(null));

            /// <summary>
            /// Exposes the command name used when the ObjectSelector is updated.
            /// </summary>
            /// <seealso cref="Event.commandName"/>
            public static string ObjectSelectorUpdatedCommand
                => (_objSelCmdUpdated ??= (string)ObjectSelectorType.GetField("ObjectSelectorUpdatedCommand", (BindingFlags)0x18).GetValue(null));

            /// <summary>
            /// Exposes the command name used when the ObjectSelector is canceled.
            /// </summary>
            /// <seealso cref="Event.commandName"/>
            public static string ObjectSelectorCanceledCommand
                => (_objSelCmdCanceled ??= (string)ObjectSelectorType.GetField("ObjectSelectorCanceledCommand", (BindingFlags)0x18).GetValue(null));

            /// <summary>
            /// Exposes the command name used when the ObjectSelector is finished with selection.
            /// </summary>
            /// <seealso cref="Event.commandName"/>
            public static string ObjectSelectorSelectionDoneCommand
                => (_objSelCmdSelectionDone ??= (string)ObjectSelectorType.GetField("ObjectSelectorSelectionDoneCommand", (BindingFlags)0x18).GetValue(null));

            /// <summary>
            /// Exposes the ID of the control which is targeted by the current ObjectSelector instance.
            /// </summary>
            public static int ObjectSelectorID
            {
                get
                {
#if UNITY_2021_3_OR_NEWER
                    return EditorGUIUtility.GetObjectPickerControlID();
#endif
                    return (int)(_objectSelectorIDProperty ??= ObjectSelectorType.GetField("objectSelectorID", (BindingFlags)0x24)).GetValue(ObjectSelectorInst);
                }
            }
        }
    }
}
