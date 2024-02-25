using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity.Events
{
    [CustomPropertyDrawer(typeof(UnityReturnDelegateBase), useForChildren: true)]
    public sealed class UnityReturnDelegateDrawer : PropertyDrawer
    {
        private const float kHeaderHeight = 18f;

        private sealed class State
        {
            public bool viewingEditorSettings;
        }

        private static readonly Dictionary<Type, UnityReturnDelegateBase> _dummyCache = new();
        private static readonly Dictionary<string, State> _stateCache = new();

        private static Type BaseType => typeof(UnityReturnDelegateBase);

        private static UnityReturnDelegateBase GetDummy(Type type)
        {
            if (!_dummyCache.TryGetValue(type, out var del))
            {
                del = (UnityReturnDelegateBase)Activator.CreateInstance(type);
                _dummyCache[type] = del;
            }
            return del;
        }

        private static State GetState(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_stateCache.TryGetValue(path, out var state))
            {
                state = new State()
                {
                    // TODO
                };
                _stateCache[path] = state;
            }
            return state;
        }

        private static Type GetReturnType(UnityReturnDelegateBase del)
        {
            const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            var property = ReflectionEx.GetProperty(BaseType, "ReturnType", kFlags);
            return (Type)property.GetValue(del);
        }

        private static string GetEventArgs(UnityReturnDelegateBase del)
        {
            const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            var method = ReflectionEx.GetMethod(BaseType, "GetEventDefinedInvocationArgumentTypes", kFlags);
            var argTypes = (Type[])method.Invoke(del, Array.Empty<object>());

            var sb = StringBuilderCache.Acquire();
            sb.Append('(');

            for (int i = 0; i < argTypes.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(argTypes[i].Name);
            }

            sb.Append(')');

            return StringBuilderCache.GetStringAndRelease(ref sb);
        }

        private void DrawHeader(Rect rect, State state, UnityReturnDelegateBase dummy, GUIContent label)
        {
            var backgroundRect = rect;
            rect = rect.PadLeft(6);

            var editorBtnLabel = new GUIContent("Editor");
            var runtimeBtnLabel = new GUIContent("Runtime");

            var tabLabelStyle = EditorGUIEx.Styles.DockArea.TabLabel;

            tabLabelStyle.CalcMinMaxWidth(editorBtnLabel, out float editorBtnSize, out _);
            tabLabelStyle.CalcMinMaxWidth(runtimeBtnLabel, out float runtimeBtnSize, out _);

            var runtimeBtnRect = rect.WithWidth(runtimeBtnSize, anchorRight: true);
            rect = rect.PadRight(runtimeBtnSize);

            var editorBtnRect = rect.WithWidth(editorBtnSize, anchorRight: true);
            rect = rect.PadRight(editorBtnSize);

            var evt = Event.current;
            var mousePos = evt.mousePosition;
            bool hoverRuntime = runtimeBtnRect.Contains(mousePos);
            bool hoverEditor = editorBtnRect.Contains(mousePos);

            if (evt.type == EventType.Repaint)
            {
                var headerStyle = ReorderableList.defaultBehaviours.headerBackground;
                headerStyle.Draw(backgroundRect, isHover: false, isActive: false, on: false, hasKeyboardFocus: false);

                var tabStyle = EditorGUIEx.Styles.DockArea.DragTab;

                bool viewingEditor = state.viewingEditorSettings;

                runtimeBtnRect.y += tabStyle.margin.top;
                tabStyle.Draw(runtimeBtnRect, isHover: hoverRuntime, isActive: !viewingEditor, on: false, hasKeyboardFocus: false);

                editorBtnRect.y += tabStyle.margin.top;
                tabStyle.Draw(editorBtnRect, isHover: hoverEditor, isActive: viewingEditor, on: false, hasKeyboardFocus: false);

                EditorGUI.LabelField(runtimeBtnRect, runtimeBtnLabel, tabLabelStyle);
                EditorGUI.LabelField(editorBtnRect, editorBtnLabel, tabLabelStyle);
            }
            else if (evt.type == EventType.MouseUp)
            {
                if (hoverRuntime) state.viewingEditorSettings = false;
                else if (hoverEditor) state.viewingEditorSettings = true;
            }

            rect.width -= editorBtnRect.width + 20f;
            rect = rect.PadTop(1);

            var returnType = GetReturnType(dummy);

            var headerContent = new GUIContent()
            {
                text = $"{returnType.Name} {label.text ?? string.Empty} {GetEventArgs(dummy)}",
                tooltip = label.tooltip
            };

            GUI.Label(rect, headerContent);
        }

        private void DrawBodyBackground(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                ReorderableList.defaultBehaviours.boxBackground.Draw(rect, isHover: false, isActive: false, on: false, hasKeyboardFocus: false);
            }
        }

        private void DrawEventCallStatesSelector(Rect rect)
        {
            // TODO: This is where Unity would allow a dropdown to specify
            // whether the callback runs in runtime mode, editor & runtime,
            // or none. Doesnt really apply to return delegates where the return
            // value is more important than the implementation, however there may
            // be reason to disable in editor.
            // Provide a way to specify what happens in editor:
            // 1. [Default] Return default value, no code is run
            // 2. Editor always returns a mock value (might combine with default).
            // 3. Run delegate as with runtime mode.

            EditorGUI.DrawRect(rect, new Color32(20, 20, 20, 255));
        }

        private bool DrawTargetTypeButton(Rect rect, bool @static)
        {
            const string kHint = "UnityReturnDelegatesTargetIcon";

            var id = GUIUtility.GetControlID(kHint.GetHashCode(), FocusType.Keyboard, rect);

            GUIContent iconContent;
            if (@static)
            {
                // TODO: Is this better if it's a lightning bolt icon, etc? Find/make something more appropriate
                iconContent = EditorGUIUtility.IconContent("d_GameObject Icon");
                iconContent.tooltip = "Targeting static members. Click to toggle.";
            }
            else
            {
                iconContent = EditorGUIUtility.IconContent("GameObject On Icon");
                iconContent.tooltip = "Targeting instance members. Click to toggle.";
            }

            return EditorGUIEx.CustomGuiButton(rect, id, EditorGUIEx.Styles.GetStyle("m_IconButton"), iconContent);
        }

        private void DrawTarget(ref Rect rect, SerializedProperty property)
        {
            var modeProp = property.FindPropertyRelative("m_targetingStaticMember");
            bool @static = modeProp.boolValue;

            var staticTargetProp = property.FindPropertyRelative("m_staticTargetType");
            var targetProp = property.FindPropertyRelative("m_target");

            var targetRect = rect;

            var typeIconRect = targetRect.WithSize(LineHeight, LineHeight);
            targetRect = targetRect.PadLeft(typeIconRect.width + Spacing);

            targetRect.height = @static
                ? SerializeTypeDefinitionDrawer.GetPropertyHeight(staticTargetProp, false)
                : LineHeight;

            rect = rect.PadTop(targetRect.height);

            if (DrawTargetTypeButton(typeIconRect, @static))
            {
                @static = !@static;
                modeProp.boolValue = @static;

                if (@static)
                {
                    targetProp.objectReferenceValue = null;
                }
                else
                {
                    SerializeTypeDefinition.EditorUtil.SetTypeDefinition(staticTargetProp, default);
                }
            }

            if (@static)
            {
                SerializeTypeDefinitionDrawer.DrawGUI(targetRect, staticTargetProp, false);
            }
            else
            {
                EditorGUI.PropertyField(targetRect, targetProp, GUIContent.none);
            }
        }

        private GenericMenu BuildStaticMemberPopupList(Type type)
        {
            Debug.LogError("Not implemented");
            return null;
        }

        private GenericMenu BuildInstanceMemberPopupList(UnityEngine.Object o)
        {
            // TODO: When displaying other components on the gameobject, have an option
            // to also append the instance id. Set this as a pref in the config (maybe at this point
            // consider a per-user prefs support in the runtime settings system, or it can just expose a bool
            // that wraps an EditorPrefs call).

            // TODO: Does the target instance box need to auto select the gameobject instead of component? See what unity does

            Debug.LogError("Not implemented");
            return null;
        }

        private void DrawMethod(Rect rect, SerializedProperty property, bool didChange)
        {
            var methodNameProp = property.FindPropertyRelative("m_methodName");

            if (didChange)
            {
                methodNameProp.stringValue = null;
            }

            EditorGUI.BeginProperty(rect, GUIContent.none, methodNameProp);

            var modeProp = property.FindPropertyRelative("m_targetingStaticMember");
            bool @static = modeProp.boolValue;

            // TODO: Thoroughly test behaviour with multi selection

            GUIContent c;
            object invocationTarget = null;

            if (modeProp.hasMultipleDifferentValues)
            {
                c = new GUIContent(
                    "Static/instance mismatch",
                    "Selected delegates target both static & instance members. Cannot edit while mismatched.");
            }
            else if (methodNameProp.hasMultipleDifferentValues)
            {
                c = EditorGUIEx.MixedValueContent;
            }
            else
            {
                var sb = StringBuilderCache.Acquire();
                
                if (@static)
                {
                    var staticTargetProp = property.FindPropertyRelative("m_staticTargetType");

                    var typeDef = SerializeTypeDefinition.EditorUtil.GetTypeDefinition(staticTargetProp);
                    var type = typeDef.IsNull ? null : typeDef.ResolveType(throwOnError: false);

                    if (type is null)
                    {
                        sb.Append("Type Unassigned");
                    }
                    else
                    {
                        invocationTarget = type;
                    }
                }
                else
                {
                    var targetProp = property.FindPropertyRelative("m_target");
                    var targetObj = targetProp.objectReferenceValue;

                    if (targetObj == null)
                    {
                        bool missingReference = targetProp.objectReferenceInstanceIDValue != 0;
                        sb.Append(missingReference ? "<Missing target object>" : "Type Unassigned");
                    }
                    else
                    {
                        invocationTarget = targetObj;
                    }
                }
                
                if (invocationTarget != null)
                {
                    if (string.IsNullOrEmpty(methodNameProp.stringValue))
                    {
                        // TODO: Should this present a warning icon? Probs
                        sb.Append("Member Unassigned");
                    }
                    // TODO: Figure out what this does in Unity's code, is it needed?
                    //else if (!IsPersistentListenerValid) { }
                    else
                    {
                        var invocationType = invocationTarget is Type t ? t : invocationTarget.GetType();

                        sb.Append(invocationType.Name);
                        sb.Append('.');

                        // TODO: Should the delegate target the property or the underlying get method?
                        if (methodNameProp.stringValue.StartsWith("get_"))
                        {
                            sb.Append(methodNameProp.stringValue.Substring(4));
                        }
                        else
                        {
                            sb.Append(methodNameProp.stringValue);
                        }
                    }
                }

                c = new GUIContent(StringBuilderCache.GetStringAndRelease(ref sb));
            }

            using (new EditorGUI.DisabledScope(disabled: invocationTarget is null))
            {
                if (EditorGUI.DropdownButton(rect, c, FocusType.Passive, EditorStyles.popup))
                {
                    if (@static)
                    {
                        Debug.Assert(invocationTarget is Type);
                        BuildStaticMemberPopupList((Type)invocationTarget).DropDown(rect);
                    }
                    else
                    {
                        Debug.Assert(invocationTarget is UnityEngine.Object);
                        BuildInstanceMemberPopupList((UnityEngine.Object)invocationTarget);
                    }
                    // TODO
                }
            }

            EditorGUI.EndProperty();
        }

        private void DrawArguments(Rect rect, SerializedProperty property)
        {

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = kHeaderHeight;

            var @static = property.FindPropertyRelative("m_targetingStaticMember").boolValue;
            var definedByEvent = property.FindPropertyRelative("m_argumentsDefinedByEvent").boolValue;

            float targetHeight = @static
                ? SerializeTypeDefinitionDrawer.GetPropertyHeight(property.FindPropertyRelative("m_staticTargetType"), false)
                : LineHeight;

            var methodAndArgsHeight = LineHeight;
            if (!definedByEvent)
            {
                // TODO: Calc arguments height
                methodAndArgsHeight += 0;
            }

            height += Spacing + Mathf.Max(targetHeight, methodAndArgsHeight);

            // Additional padding around body content
            height += 10;

            // TODO: Arguments expanded? add height

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var state = GetState(property);

            // TODO: Maybe put this in the State object?
            var resolver = new UnityEditorHelper.SerializedPropertyResolver(property);
            var member = resolver.GetSerializedMember();
            var eventType = member.MemberType;

            var dummy = GetDummy(eventType);

            var headerRect = position.WithHeight(kHeaderHeight);
            position = position.PadTop(headerRect.height + Spacing);

            DrawHeader(headerRect, state, dummy, label);

            DrawBodyBackground(position);
            position = position.Pad(left: 4, right: 4, top: 6, bottom: 4);

            if (state.viewingEditorSettings)
            {
                // TODO: Option to
                // 1. [Default] return a mock value specifically for editor
                // 2. Invoke as normal
                // If returning mock value, allow it to be set in editor if its a serializable type,
                // otherwise show a label warning that 'default' value will be returned
            }
            else
            {
                var leftColumnRect = position.WithWidth(position.width * 0.38f);
                var rightColumnRect = position.PadLeft(leftColumnRect.width + Spacing + Spacing);

                EditorGUI.BeginChangeCheck();

                DrawTarget(ref leftColumnRect, property);

                bool targetChanged = EditorGUI.EndChangeCheck();

                // TODO: Does this need to calculate method content height or just pass by ref like DrawTarget?
                var methodRect = rightColumnRect;

                DrawMethod(methodRect, property, targetChanged);
            }
        }
    }
}
