using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
            public bool viewingAdvancedSettings;
        }

        private static readonly GUIContent[] _policyOptions = new GUIContent[4]
        {
            new GUIContent(
                "Global (Default)",
                "Use global error handling policy."),
            new GUIContent(
                "None",
                "Ignore errors. Invocation does not proceed, and the default value is returned."),
            new GUIContent(
                "Log Error",
                "Log an error. Invocation does not proceed, and the default value is returned."),
            new GUIContent(
                "Log Exception",
                "An exception of type " + nameof(InvocationTargetDestroyedException) + " is thrown")
        };

        private static readonly GUIContent[] _editorInvocationOptions = new GUIContent[2]
        {
            new GUIContent(
                "Return Mock Value",
                "Delegate is not invoked in Edit mode. Instead, a mock value is returned."),
            new GUIContent(
                "Invoke Delegate",
                "Delegate is invoked as normal in Edit mode.")
        };

        private static readonly Dictionary<Type, UnityReturnDelegateBase> _dummyCache = new();
        private static readonly Dictionary<string, ValueMemberInfo> _memberCache = new();
        private static readonly Dictionary<string, State> _stateCache = new();

        private static Type BaseType => typeof(UnityReturnDelegateBase);

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void OnRecompile()
        {
            _dummyCache.Clear();
            _memberCache.Clear();
            _stateCache.Clear();
        }

        private static int CompareDisplayOrder(MemberInfo x, MemberInfo y)
        {
            bool xIsProperty = x is PropertyInfo;
            bool yIsProperty = y is PropertyInfo;

            int comp = xIsProperty.CompareTo(yIsProperty);
            if (comp == 0)
            {
                comp = StringComparer.Ordinal.Compare(x.Name, y.Name);
            }

            return comp;
        }

        private static UnityReturnDelegateBase GetDummy(Type type)
        {
            if (!_dummyCache.TryGetValue(type, out var del))
            {
                del = (UnityReturnDelegateBase)Activator.CreateInstance(type);
                _dummyCache[type] = del;
            }
            return del;
        }

        private static ValueMemberInfo GetMember(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_memberCache.TryGetValue(path, out var member))
            {
                var resolver = new UnityEditorHelper.SerializedPropertyResolver(property);
                member = resolver.GetSerializedMember();
                _memberCache[path] = member;
            }
            return member;
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

        private static string GetNiceTypeName(Type type)
        {
            if (type == typeof(bool)) return "bool";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(char)) return "char";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(short)) return "short";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(nint)) return "nint";
            if (type == typeof(nuint)) return "nuint";
            if (type == typeof(string)) return "string";
            if (type == typeof(object)) return "object";
            if (type == typeof(UnityEngine.Object)) return "UnityEngine.Object";
            return type.Name;
        }

        private static Type GetReturnType(UnityReturnDelegateBase del)
        {
            const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            var property = ReflectionEx.GetProperty(BaseType, "ReturnType", kFlags);
            return (Type)property.GetValue(del);
        }

        private static void GetArgumentString<T>(T argTypes, StringBuilder sb)
            where T : IEnumerable<Type>
        {
            sb.Append('(');

            bool flag = false;
            foreach (var t in argTypes)
            {
                if (flag) sb.Append(", ");
                sb.Append(GetNiceTypeName(t));

                flag = true;
            }

            sb.Append(')');
        }

        private static string GetArgumentString<T>(T argTypes)
            where T : IEnumerable<Type>
        {
            var sb = StringBuilderCache.Acquire();
            GetArgumentString(argTypes, sb);

            return StringBuilderCache.GetStringAndRelease(ref sb);
        }

        private void DrawHeader(Rect rect, State state, UnityReturnDelegateBase dummy, GUIContent label)
        {
            var backgroundRect = rect;
            rect = rect.PadLeft(6);

            var advancedLabel = new GUIContent("Advanced");
            var basicLabel = new GUIContent("Basic");

            var tabLabelStyle = EditorGUIEx.Styles.DockArea.TabLabel;

            tabLabelStyle.CalcMinMaxWidth(advancedLabel, out float advancedSize, out _);
            tabLabelStyle.CalcMinMaxWidth(basicLabel, out float basicSize, out _);

            var advancedBtnRect = rect.WithWidth(advancedSize, anchorRight: true);
            rect = rect.PadRight(advancedSize);

            var basicBtnRect = rect.WithWidth(basicSize, anchorRight: true);
            rect = rect.PadRight(basicSize);

            var evt = Event.current;
            var mousePos = evt.mousePosition;
            bool hoverRuntime = basicBtnRect.Contains(mousePos);
            bool hoverEditor = advancedBtnRect.Contains(mousePos);

            if (evt.type == EventType.Repaint)
            {
                var headerStyle = ReorderableList.defaultBehaviours.headerBackground;
                headerStyle.Draw(backgroundRect, isHover: false, isActive: false, on: false, hasKeyboardFocus: false);

                var tabStyle = EditorGUIEx.Styles.DockArea.DragTab;

                bool isViewingAdvanced = state.viewingAdvancedSettings;

                basicBtnRect.y += tabStyle.margin.top;
                tabStyle.Draw(basicBtnRect, isHover: hoverRuntime, isActive: !isViewingAdvanced, on: false, hasKeyboardFocus: false);

                advancedBtnRect.y += tabStyle.margin.top;
                tabStyle.Draw(advancedBtnRect, isHover: hoverEditor, isActive: isViewingAdvanced, on: false, hasKeyboardFocus: false);

                EditorGUI.LabelField(basicBtnRect, basicLabel, tabLabelStyle);
                EditorGUI.LabelField(advancedBtnRect, advancedLabel, tabLabelStyle);
            }
            else if (evt.type == EventType.MouseUp)
            {
                if (hoverRuntime) state.viewingAdvancedSettings = false;
                else if (hoverEditor) state.viewingAdvancedSettings = true;
            }

            rect.width -= advancedBtnRect.width + 20f;
            rect = rect.PadTop(1);

            var returnType = GetReturnType(dummy);

            var dynamicParameters = dummy.GetEventDefinedInvocationArgumentTypes();
            var headerContent = new GUIContent()
            {
                text = $"{returnType.Name} {label.text ?? string.Empty} {GetArgumentString(dynamicParameters)}",
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

        private void DrawAdvancedSettings(Rect rect, SerializedProperty property)
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

            // TODO: Also consider putting a help box here with info stating that
            // invoking runtime logic may be dangerous.

            var policyProp = property.FindPropertyRelative("m_policy");
            var policy = policyProp.intValue;

            var policyRect = rect.WithHeight(LineHeight);
            rect = rect.PadTop(LineHeight + Spacing);

            var behaviourProp = property.FindPropertyRelative("m_editorBehaviour");
            var behaviour = behaviourProp.intValue;

            var behaviourRect = rect.WithHeight(LineHeight);
            rect = rect.PadTop(LineHeight + Spacing);

            // TODO: Tooltip. is this just for target obj destroyed?
            var labelContent = GetTempContent(
                "Error Handling Policy",
                "");
            policyRect = EditorGUI.PrefixLabel(policyRect, labelContent);

            EditorGUI.BeginChangeCheck();
            policy = EditorGUI.Popup(policyRect, policy, _policyOptions);

            if (EditorGUI.EndChangeCheck()) policyProp.intValue = policy;

            behaviourRect = EditorGUI.PrefixLabel(behaviourRect, GetTempContent("Editor Behaviour"));

            EditorGUI.BeginChangeCheck();
            behaviour = EditorGUI.Popup(behaviourRect, behaviour, _editorInvocationOptions);

            if (EditorGUI.EndChangeCheck()) behaviourProp.intValue = behaviour;

            // TODO: Mock value area
        }

        private bool DrawTargetTypeButton(Rect rect, bool @static)
        {
            const string kHint = "UnityReturnDelegatesTargetIcon";

            var id = GUIUtility.GetControlID(kHint.GetHashCode(), FocusType.Keyboard, rect);

            GUIContent iconContent;
            if (@static)
            {
                iconContent = new GUIContent(EditorGUIEx.Icons.GameObjectStatic);
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

        private static List<MemberInfo> GetMembersWithReturnType(Type declaringType, Type returnType)
        {
            var list = new List<MemberInfo>();

            foreach (var m in declaringType.GetMethods())
                if (!m.IsSpecialName &&
                    returnType.IsAssignableFrom(m.ReturnType))
                {
                    list.Add(m);
                }

            foreach (var p in declaringType.GetProperties())
            {
                var m = p.GetGetMethod();
                if (m != null &&
                    returnType.IsAssignableFrom(m.ReturnType) &&
                    p.GetCustomAttribute<ObsoleteAttribute>() == null &&
                    m.GetCustomAttribute<ObsoleteAttribute>() == null)
                {
                    list.Add(p);
                }
            }

            return list;
        }

        private static string GetNiceMemberString(MemberInfo member, bool dynamic)
        {
            if (member is PropertyInfo p)
            {
                return dynamic ? p.Name : $"{GetNiceTypeName(p.PropertyType)} {p.Name}";
            }

            if (dynamic) return member.Name;

            var method = member as MethodInfo;
            var paramTypes = System.Linq.Enumerable.Select(method.GetParameters(), p => p.ParameterType);

            var sb = StringBuilderCache.Acquire();

            sb.Append(GetNiceTypeName(method.ReturnType));
            sb.Append(' ');
            sb.Append(member.Name);
            GetArgumentString(paramTypes, sb);

            return StringBuilderCache.GetStringAndRelease(ref sb);
        }

        private GenericMenu BuildStaticMemberPopupList(Type declaringType, SerializedProperty property)
        {
            // TODO: Idea, Invocation Argument base class is used for all the
            // serializable arguments. If there was a system that could bind
            // these types, the system could be extended to support methods
            // that have more parameter types than just the unity-supported ones!
            // Or... All reference types are supported out of the box.
            // Structs can be bound by user as required...

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("None"), false, null);

            var serializedMember = GetMember(property);
            var dummy = GetDummy(serializedMember.MemberType);
            var returnType = GetReturnType(dummy);

            var list = GetMembersWithReturnType(declaringType, returnType);

            list.RemoveAll(m =>
            {
                bool isStatic = (m is PropertyInfo p)
                    ? p.GetGetMethod().IsStatic
                    : (m as MethodInfo).IsStatic;

                return !isStatic;
            });
            list.Sort(CompareDisplayOrder);

            var dynamicParams = dummy.GetEventDefinedInvocationArgumentTypes();
            var dynamicParameterCount = dynamicParams.Length;

            var list2 = new List<MemberInfo>();
            foreach (var m in list)
            {
                bool match = false;
                if (m is PropertyInfo prop)
                {
                    match = dynamicParameterCount == 0;
                }
                else if (m is MethodInfo method)
                {
                    match = true;
                    var methodParams = method.GetParameters();
                    if (dynamicParameterCount == methodParams.Length)
                    {
                        for (int i = 0; i < dynamicParameterCount; ++i)
                            if (!dynamicParams[i].IsAssignableFrom(methodParams[i].ParameterType))
                            {
                                match = false;
                                break;
                            }
                    }
                    else
                    {
                        match = false;
                    }
                }

                if (match)
                {
                    list2.Add(m);
                }
            }

            bool anyDynamicMethods = false;
            if (list2.Count > 0)
            {
                anyDynamicMethods = true;

                // TODO: Determine if the return type should show before "Dynamic" string
                var sb = StringBuilderCache.Acquire();
                sb.Append("Dynamic ");
                GetArgumentString(dynamicParams, sb);

                menu.AddItem(new GUIContent(sb.ToString()), false, null);

                StringBuilderCache.Release(ref sb);

                foreach (var m in list2)
                {
                    // TODO: On state, callback.
                    menu.AddItem(new GUIContent(GetNiceMemberString(m, true)), false, null);
                }
            }

            list2.Clear();

            foreach (var m in list)
            {
                // TODO: Match static args. Just anything thats actually serializable as InvocationArgument.
                list2.Add(m);
            }

            if (list2.Count > 0)
            {
                if (anyDynamicMethods)
                {
                    menu.AddSeparator(string.Empty);
                }

                menu.AddItem(new GUIContent("Static Arguments"), false, null);

                foreach (var m in list2)
                {
                    // TODO: On state, callback.
                    menu.AddItem(new GUIContent(GetNiceMemberString(m, false)), false, null);
                }
            }

            return menu;
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

                        if (methodNameProp.stringValue.StartsWith("get_"))
                        {
                            var span = methodNameProp.stringValue.AsSpan();
                            sb.Append(span.Slice(4));
                        }
                        else
                        {
                            sb.Append(methodNameProp.stringValue);
                        }
                    }
                }

                c = new GUIContent(StringBuilderCache.GetStringAndRelease(ref sb));
            }

            // TODO: Have an icon indicating if the current assigned method is dynamic or static args.
            using (new EditorGUI.DisabledScope(disabled: invocationTarget is null))
            {
                if (EditorGUI.DropdownButton(rect, c, FocusType.Passive, EditorStyles.popup))
                {
                    if (@static)
                    {
                        Debug.Assert(invocationTarget is Type);
                        BuildStaticMemberPopupList((Type)invocationTarget, property).DropDown(rect);
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
            // TODO: Use an AnimBool etc when swapping between basic/advanced settings

            var state = GetState(property);

            // Header content + body padding
            float height = kHeaderHeight + 10f + Spacing;

            if (state.viewingAdvancedSettings)
            {
                height += LineHeight + LineHeight + Spacing;
                // TODO: Args height
            }
            else
            {
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
            }

            // TODO: Arguments expanded? add height

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var state = GetState(property);
            var member = GetMember(property);
            var eventType = member.MemberType;
            var dummy = GetDummy(eventType);

            var headerRect = position.WithHeight(kHeaderHeight);
            position = position.PadTop(headerRect.height + Spacing);

            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                DrawHeader(headerRect, state, dummy, label);

                DrawBodyBackground(position);
                position = position.Pad(left: 4, right: 4, top: 6, bottom: 4);

                if (state.viewingAdvancedSettings)
                {
                    DrawAdvancedSettings(position, property);

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
                    var methodRect = rightColumnRect.WithHeight(LineHeight);

                    DrawMethod(methodRect, property, targetChanged);
                }
            }
        }
    }
}
