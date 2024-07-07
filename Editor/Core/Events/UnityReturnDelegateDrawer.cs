using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;
using static JakePerry.Unity.Events.ReturnDelegatesEditorUtil;
using static JakePerry.Unity.Events.UnityReturnDelegateBase;

namespace JakePerry.Unity.Events
{
    [CustomPropertyDrawer(typeof(UnityReturnDelegateBase), useForChildren: true)]
    public sealed class UnityReturnDelegateDrawer : PropertyDrawer
    {
        private const string kErrorPolicyTooltip = "Policy used when invocation fails due to an exception.";
        private const string kWarnInvokeInEditorIsDangerousMessage = "Executing runtime logic while the game is not running may be erroneous and potentially cause unwanted modifications to serialized data.";
        private const string kEditorBehaviourTooltip = "Behaviour when the delegate is invoked outside of Play Mode in the Editor.\nPlease note: " + kWarnInvokeInEditorIsDangerousMessage;
        private const string kMockingNotSerializableMessage = "Return type is not serializable. Default value will be used.";

        private const string kBasicSettingsTabHint = "UnityReturnDelegateDrawer.Tab.Basic";
        private const string kAdvancedSettingsTabHint = "UnityReturnDelegateDrawer.Tab.Advanced";

        private const float kHeaderHeight = 18f;
        private const float kNextElementSpacing = 1f;

        private static readonly GUIContent[] _errorPolicyOptions = new GUIContent[4]
        {
            new GUIContent(
                "Default (Global)",
                "Use the global error handling policy for this error."),
            new GUIContent(
                "Ignore Error",
                "Ignore the error. Delegate invocation does not proceed, and the default value is returned."),
            new GUIContent(
                "Log Error",
                "Log an error. Delegate invocation does not proceed, and the default value is returned."),
            new GUIContent(
                "Throw Exception",
                "Throw an exception to halt delegate invocation. The thrown exception is always of type " +
                nameof(InvokeFailedException) + " or a child type and can be caught as such.")
        };

        private sealed class State
        {
            public bool viewingAdvancedSettings;
        }

        private sealed class PropertyCache
        {
            public readonly SerializedProperty property;

            public readonly SerializedProperty target;
            public readonly SerializedProperty staticTargetType;
            public readonly SerializedProperty targetingStaticMember;
            public readonly SerializedProperty methodName;
            public readonly SerializedProperty arguments;
            public readonly SerializedProperty argumentsDefinedByEvent;
            public readonly SerializedProperty invocationFailedPolicy;
            public readonly SerializedProperty editorBehaviour;
            public readonly SerializedProperty editorMockValue;

            public PropertyCache(SerializedProperty property)
            {
                this.property = property;

                target = property.FindPropertyRelative("m_target");
                staticTargetType = property.FindPropertyRelative("m_staticTargetType");
                targetingStaticMember = property.FindPropertyRelative("m_targetingStaticMember");
                methodName = property.FindPropertyRelative("m_methodName");
                arguments = property.FindPropertyRelative("m_arguments");
                argumentsDefinedByEvent = property.FindPropertyRelative("m_argumentsDefinedByEvent");

                invocationFailedPolicy = property.FindPropertyRelative("m_invocationFailedPolicy");
                editorBehaviour = property.FindPropertyRelative("m_editorBehaviour");
                editorMockValue = property.FindPropertyRelative("m_editorMockValue");
            }
        }

        private sealed class AssignMethodArguments
        {
            public PropertyCache properties;
            public MemberInfo member;
            public bool dynamicArguments;
        }

        private readonly struct Context
        {
            public readonly PropertyCache properties;
            public readonly ValueMemberInfo serializedMember;
            public readonly DelegateMetadata metadata;
            public readonly State state;

            public Context(PropertyCache properties, ValueMemberInfo serializedMember, DelegateMetadata metadata, State state)
            {
                this.properties = properties;
                this.serializedMember = serializedMember;
                this.metadata = metadata;
                this.state = state;
            }
        }

        private static readonly GenericMenu.MenuFunction2 _assignMethodCallback = AssignMethod;

        private static readonly Dictionary<string, PropertyCache> _propertyCache = new();
        private static readonly Dictionary<string, ValueMemberInfo> _memberCache = new();
        private static readonly Dictionary<string, State> _stateCache = new();

        private static Context _context;

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void OnRecompile()
        {
            _propertyCache.Clear();
            _memberCache.Clear();
            _stateCache.Clear();
        }

        private static ValueMemberInfo GetMember(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_memberCache.TryGetValue(path, out var member))
            {
                member = PropertyPathWalker.GetFieldOrProperty(property);
                _memberCache[path] = member;
            }
            return member;
        }

        private static void ValidateSerializedData(SerializedProperty property)
        {
            // TODO: More stuff probably should be validated here
        }

        private static PropertyCache GetChildProperties(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_propertyCache.TryGetValue(path, out var cache) ||
                cache.property.serializedObject != property.serializedObject)
            {
                cache = new PropertyCache(property);
                _propertyCache[path] = cache;
            }
            return cache;
        }

        private static State GetState(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_stateCache.TryGetValue(path, out var state))
            {
                state = new State();
                _stateCache[path] = state;
            }
            return state;
        }

        private static void DrawHintIcon(Rect rect, string tooltip, MessageType iconType = MessageType.Error)
        {
            if (Event.current.type != EventType.Repaint) return;

            var icon = UnityEditorHelper.GetMessageIcon(iconType);
            var iconStyle = EditorStyles.iconButton;
            rect = iconStyle.margin.Remove(rect);

            var hintContent = GetTempContent(icon);
            hintContent.tooltip = tooltip;

            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                EditorGUI.LabelField(rect, hintContent, iconStyle);
            }
        }

        private static void AssignMethod(object e)
        {
            var args = (AssignMethodArguments)e;

            args.properties.methodName.stringValue = args.member?.Name ?? string.Empty;
            args.properties.argumentsDefinedByEvent.boolValue = args.dynamicArguments;

            // TODO: Handle argument types properly.
            if (args.dynamicArguments)
            {
                args.properties.arguments.ClearArray();
            }

            args.properties.property.serializedObject.ApplyModifiedProperties();
        }

        private static void AddMemberSelectOption(
            GenericMenu menu,
            string name,
            bool on,
            PropertyCache properties,
            MemberInfo member,
            bool dynamicArguments)
        {
            var args = new AssignMethodArguments()
            {
                properties = properties,
                member = member,
                dynamicArguments = dynamicArguments
            };

            menu.AddItem(new GUIContent(name), on, _assignMethodCallback, args);
        }

        private static bool IsGameObjectOrComponentReference(SerializedProperty target)
        {
            var o = target.objectReferenceValue;
            return o != null && (o is GameObject || o is Component);
        }

        private void DrawHeader(Rect rect, GUIContent label)
        {
            // TODO: Consider supporting argument coloring for header signature

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

            int basicBtnId = GUIUtility.GetControlID(kBasicSettingsTabHint.GetHashCode(), FocusType.Keyboard, basicBtnRect);
            int advancedBtnId = GUIUtility.GetControlID(kAdvancedSettingsTabHint.GetHashCode(), FocusType.Keyboard, advancedBtnRect);

            var evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                var headerStyle = ReorderableList.defaultBehaviours.headerBackground;
                headerStyle.Draw(backgroundRect, isHover: false, isActive: false, on: false, hasKeyboardFocus: false);

                var tabStyle = EditorGUIEx.Styles.DockArea.DragTab;

                var mousePos = evt.mousePosition;
                bool isViewingAdvanced = _context.state.viewingAdvancedSettings;

                basicBtnRect.y += tabStyle.margin.top;
                tabStyle.Draw(basicBtnRect, isHover: basicBtnRect.Contains(mousePos), isActive: !isViewingAdvanced, on: false, hasKeyboardFocus: GUIUtility.keyboardControl == basicBtnId);

                advancedBtnRect.y += tabStyle.margin.top;
                tabStyle.Draw(advancedBtnRect, isHover: advancedBtnRect.Contains(mousePos), isActive: isViewingAdvanced, on: false, hasKeyboardFocus: GUIUtility.keyboardControl == advancedBtnId);

                EditorGUI.LabelField(basicBtnRect, basicLabel, tabLabelStyle);
                EditorGUI.LabelField(advancedBtnRect, advancedLabel, tabLabelStyle);
            }
            else
            {
                if (EditorGUIEx.ProcessGuiClickEvent(evt, basicBtnRect, basicBtnId))
                {
                    _context.state.viewingAdvancedSettings = false;
                }
                if (EditorGUIEx.ProcessGuiClickEvent(evt, advancedBtnRect, advancedBtnId))
                {
                    _context.state.viewingAdvancedSettings = true;
                }
            }

            rect.width -= advancedBtnRect.width + 20f;
            rect = rect.PadTop(1);

            var metadata = _context.metadata;

            var dynamicParameters = metadata.eventDefinedArgs;

            var sb = StringBuilderCache.Acquire();

            // TODO: Cache GetArgumentString result in the metadata, or perhaps this entire header string.
            sb.Append(GetNiceTypeName(metadata.returnType));
            sb.Append(' ');
            sb.Append(string.IsNullOrEmpty(label.text) ? "Delegate" : label.text);
            GetArgumentString(dynamicParameters, sb);

            var contentText = StringBuilderCache.GetStringAndRelease(sb);

            var headerContent = new GUIContent()
            {
                text = contentText,
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

        private void DrawAdvancedSettings(Rect rect)
        {
            var errorPolicyProp = _context.properties.invocationFailedPolicy;
            var behaviourProp = _context.properties.editorBehaviour;

            var errorPolicy = errorPolicyProp.intValue;
            var behaviour = behaviourProp.intValue;
            var mockProp = _context.properties.editorMockValue;

            var policyRect = rect.WithHeight(LineHeight);
            rect = rect.PadTop(LineHeight + Spacing);

            var behaviourRect = rect.WithHeight(LineHeight);
            rect = rect.PadTop(LineHeight + Spacing);

            var labelContent = GetTempContent(
                text: "Error Handling Policy",
                tooltip: kErrorPolicyTooltip);
            policyRect = EditorGUI.PrefixLabel(policyRect, labelContent);

            EditorGUI.BeginChangeCheck();
            errorPolicy = EditorGUI.Popup(policyRect, errorPolicy, _errorPolicyOptions);
            if (EditorGUI.EndChangeCheck()) errorPolicyProp.intValue = errorPolicy;

            if (behaviour == EditorBehaviours.kReturnMockValue && mockProp == null)
            {
                var hintRect = behaviourRect.WithWidth(LineHeight, anchorRight: true);
                behaviourRect = behaviourRect.PadRight(LineHeight + Spacing);
                DrawHintIcon(hintRect, kMockingNotSerializableMessage, MessageType.Warning);
            }
            else if (behaviour == EditorBehaviours.kInvokeInEditMode)
            {
                var hintRect = behaviourRect.WithWidth(LineHeight, anchorRight: true);
                behaviourRect = behaviourRect.PadRight(LineHeight + Spacing);
                DrawHintIcon(hintRect, kWarnInvokeInEditorIsDangerousMessage, MessageType.Warning);
            }

            labelContent = GetTempContent(
                text: "Editor Behaviour",
                tooltip: kEditorBehaviourTooltip);
            behaviourRect = EditorGUI.PrefixLabel(behaviourRect, labelContent);

            EditorGUI.BeginChangeCheck();
            behaviour = EditorGUI.Popup(behaviourRect, behaviour, EditorInvocationOptions);

            if (EditorGUI.EndChangeCheck())
            {
                behaviourProp.intValue = behaviour;
                if (mockProp != null)
                {
                    // TODO: Investigate why this seemingly isnt saving. Logs indicate the value is applied,
                    // the target object is dirty, and the value persists until the end of OnGUI call.
                    // The next gui, it's back to the last value.
                    // I also tried Activator.CreateInstance to create a new obj in case null was tripping it up,
                    // but that didnt help at all. The custom drawer for SerDataClass was also disabled.
                    PropertyPathWalker.SetDefaultValue(mockProp);
                }
            }

            if (behaviour == EditorBehaviours.kReturnMockValue)
            {
                if (mockProp != null)
                {
                    var mockValueRect = rect.WithHeight(EditorGUI.GetPropertyHeight(mockProp, true));

                    // TODO: Check this when nested, make sure indent is correct.
                    EditorGUI.indentLevel++;
                    {
                        mockValueRect = EditorGUI.PrefixLabel(mockValueRect, GetTempContent("Mock Value"));
                    }
                    EditorGUI.indentLevel--;

                    EditorGUI.PropertyField(mockValueRect, mockProp, GUIContent.none, true);

                    rect = rect.PadTop(mockValueRect.height + Spacing);
                }
            }
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

        private void TargetComponentDropdown(Rect rect, UnityEngine.Object objRef, SerializedProperty targetProp)
        {
            var gameObj = objRef is GameObject go ? go : (objRef as Component).gameObject;

            var dropdownMenu = new GenericMenu();
            var callback = new GenericMenu.MenuFunction2(o =>
            {
                targetProp.objectReferenceValue = (UnityEngine.Object)o;
                targetProp.serializedObject.ApplyModifiedProperties();
            });

            dropdownMenu.AddDisabledItem(new GUIContent(objRef.name));
            dropdownMenu.AddSeparator(string.Empty);

            dropdownMenu.AddItem(new GUIContent("GameObject"), gameObj == objRef, callback, gameObj);

            var countLookup = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var c in gameObj.GetComponents(typeof(Component)))
            {
                var name = c.GetType().Name;
                if (countLookup.TryGetValue(name, out int count))
                {
                    name = $"{name} ({count})";
                }

                countLookup[name] = count + 1;

                var capture = c;
                dropdownMenu.AddItem(new GUIContent(name), c == objRef, callback, capture);
            }

            dropdownMenu.DropDown(rect);
        }

        private void DrawTarget(ref Rect rect)
        {
            var modeProp = _context.properties.targetingStaticMember;
            var staticTargetProp = _context.properties.staticTargetType;
            var targetProp = _context.properties.target;

            bool @static = modeProp.boolValue;

            var targetRect = rect;

            var typeIconRect = targetRect.WithSize(LineHeight, LineHeight);
            targetRect = targetRect.PadLeft(typeIconRect.width + Spacing);

            if (@static)
            {
                targetRect.height = SerializeTypeDefinitionDrawer.GetPropertyHeight(staticTargetProp, false);
            }
            else
            {
                targetRect.height = IsGameObjectOrComponentReference(targetProp)
                    ? LineHeight + LineHeight + Spacing
                    : LineHeight;
            }

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
                targetRect.height = LineHeight;

                EditorGUI.PropertyField(targetRect, targetProp, GUIContent.none);

                if (IsGameObjectOrComponentReference(targetProp))
                {
                    var componentRect = targetRect.OffsetY(LineHeight + Spacing);
                    var objRef = targetProp.objectReferenceValue;

                    if (EditorGUI.DropdownButton(componentRect, GetTempContent(objRef.GetType().Name), FocusType.Keyboard))
                    {
                        TargetComponentDropdown(componentRect, objRef, targetProp);
                    }
                }
            }
        }

        private static List<MemberInfo> GetMembersWithReturnType(Type declaringType, Type returnType, BindingFlags bindingAttr)
        {
            var list = new List<MemberInfo>();

            foreach (var m in declaringType.GetMethods(bindingAttr))
                if (!m.IsSpecialName &&
                    returnType.IsAssignableFrom(m.ReturnType))
                {
                    // TODO: Handle obsolete, same as below
                    if (m.GetCustomAttribute<ObsoleteAttribute>() == null)
                    {
                        list.Add(m);
                    }
                }

            foreach (var p in declaringType.GetProperties(bindingAttr))
            {
                var m = p.GetGetMethod();
                if (m != null && returnType.IsAssignableFrom(m.ReturnType))
                {
                    // TODO: Settings option to show or hide Obsolete methods. Prefix with [Obsolete]
                    if (p.GetCustomAttribute<ObsoleteAttribute>() == null &&
                        m.GetCustomAttribute<ObsoleteAttribute>() == null)
                    {
                        list.Add(p);
                    }
                }
            }

            return list;
        }

        private static string GetNiceMemberString(MemberInfo member, bool includeReturnType)
        {
            if (member is PropertyInfo p)
            {
                return GetNicePropertyString(p, includeReturnType, PropertyMethodType.Get);
            }

            return GetNiceMethodString(member as MethodInfo, includeReturnType);
        }

        private GenericMenu BuildMemberPopupList(Type declaringType, MethodInfo currentMethod, bool @static)
        {
            var metadata = _context.metadata;
            var properties = _context.properties;
            var definedByEvent = _context.properties.argumentsDefinedByEvent.boolValue;

            var menu = new GenericMenu();

            AddMemberSelectOption(menu, "None", currentMethod is null, properties, null, true);

            // TODO: Make a decision as to whether or not private members can/should be supported for non-static targets.
            // Probably yes. When supported, would be nice to show the access modifier in the dropdown, which will
            // require something other than GenericMenu :(

            // Get all invocable methods (including property 'get' methods)
            var bindingAttr = (@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public;
            var list = GetMembersWithReturnType(declaringType, metadata.returnType, bindingAttr);

            // Sort the list
            list.Sort(CompareMemberDisplayOrder);

            var dynamicParams = metadata.eventDefinedArgs;
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

            if (list2.Count > 0)
            {
                menu.AddSeparator(string.Empty);

                menu.AddItem(new GUIContent("Dynamic Arguments"), false, null);

                foreach (var m in list2)
                {
                    bool on = currentMethod == m && definedByEvent;
                    AddMemberSelectOption(menu, GetNiceMemberString(m, false), on, properties, m, true);
                }
            }

            list2.Clear();

            bool anyReturnsSubclass = false;
            foreach (var m in list)
            {
                if (!anyReturnsSubclass)
                {
                    anyReturnsSubclass = ((m is PropertyInfo p) ? p.PropertyType : (m as MethodInfo).ReturnType) != metadata.returnType;
                }

                // TODO: Instead of filtering these here, maybe allow them to be selected
                // but draw the argument as a single-line warning, directing the user about
                // how to add support for serializing the argument.
                if (m is MethodInfo method)
                    foreach (var param in method.GetParameters())
                    {
                        if (!IsMemberParameterSerializable(param.ParameterType))
                        {
                            goto SKIP_MEMBER;
                        }
                    }

                list2.Add(m);

            SKIP_MEMBER:
                continue;
            }

            if (list2.Count > 0)
            {
                // Only display return types of the methods if one or more methods return a type that
                // is not exactly equal to the expected type (ie. a subclass or interface implementation).
                bool includeReturnType = anyReturnsSubclass;

                menu.AddSeparator(string.Empty);

                menu.AddItem(new GUIContent("Static Arguments"), false, null);

                foreach (var m in list2)
                {
                    bool on = currentMethod == m && !definedByEvent;
                    AddMemberSelectOption(menu, GetNiceMemberString(m, includeReturnType), on, properties, m, false);
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

        private void DrawMethod(Rect rect)
        {
            var property = _context.properties.property;

            var methodNameProp = _context.properties.methodName;
            var modeProp = _context.properties.targetingStaticMember;

            EditorGUI.BeginProperty(rect, GUIContent.none, methodNameProp);

            bool @static = modeProp.boolValue;

            // TODO: Thoroughly test behaviour with multi selection

            GUIContent c;
            object invocationTarget = null;
            MethodInfo currentMethod = null;
            string methodResolveError = null;

            if (modeProp.hasMultipleDifferentValues)
            {
                c = new GUIContent(
                    "Static/instance mismatch",
                    "Selected delegates target both static & instance members. Cannot edit while mismatched.");
            }
            else if (methodNameProp.hasMultipleDifferentValues)
            {
                // TODO: Validation check if any of the targets have unresolvable method.
                //       This can be done by iterating property.serializedObject.targets,
                //       Creating new SerializedObject for each of them, then checking as is done below.
                c = EditorGUIEx.MixedValueContent;
            }
            else
            {
                var sb = StringBuilderCache.Acquire();

                if (@static)
                {
                    var typeDef = SerializeTypeDefinition.EditorUtil.GetTypeDefinition(_context.properties.staticTargetType);
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
                    var targetProp = _context.properties.target;
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
                        var metadata = _context.metadata;

                        var invocationType = invocationTarget is Type t ? t : invocationTarget.GetType();

                        Type[] currentArgumentTypes;
                        var definedByEvent = _context.properties.argumentsDefinedByEvent.boolValue;
                        if (definedByEvent)
                        {
                            currentArgumentTypes = metadata.eventDefinedArgs;
                        }
                        else
                        {
                            // TODO: Get the arg types. UnityReturnDelegateBase.GetCachedInvocationArgumentTypes
                            // does this but we don't actually have the InvocationArgument[] at this point (wrapped in SerializedProperty).
                            // Consider breaking it out to work per-type. Still gotta account for multi-values here.
                            currentArgumentTypes = Array.Empty<Type>();
                        }

                        currentMethod = GetValidMethodInfo(invocationType, @static, methodNameProp.stringValue, metadata.returnType, currentArgumentTypes, out methodResolveError);

                        bool methodIsMissing = currentMethod is null && !string.IsNullOrEmpty(methodNameProp.stringValue);
                        if (methodIsMissing)
                        {
                            if (methodResolveError is null)
                            {
                                methodResolveError =
                                    "Method was not found. This may indicate that the method has been refactored since this delegate was serialized.";
                            }

                            sb.Append("<Missing> ");
                        }

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

                        if (methodIsMissing && definedByEvent)
                        {
                            // TODO: Append serialized argument types. This may help debug why it's missing.
                        }
                    }
                }

                c = new GUIContent(StringBuilderCache.GetStringAndRelease(sb));
            }

            if (methodResolveError is not null)
            {
                var hintRect = rect.WithWidth(LineHeight, anchorRight: true);
                rect = rect.PadRight(hintRect.width + Spacing);

                DrawHintIcon(hintRect, methodResolveError);
            }

            // TODO: Have an icon indicating if the current assigned method is dynamic or static args.
            using (new EditorGUI.DisabledScope(disabled: invocationTarget is null))
            {
                if (EditorGUI.DropdownButton(rect, c, FocusType.Passive, EditorStyles.popup))
                {
                    Type declaringType;
                    if (@static)
                    {
                        Debug.Assert(invocationTarget is Type);
                        declaringType = (Type)invocationTarget;
                    }
                    else
                    {
                        Debug.Assert(invocationTarget is UnityEngine.Object);
                        declaringType = invocationTarget.GetType();
                    }

                    BuildMemberPopupList(declaringType, currentMethod, @static).DropDown(rect);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // TODO: Use an AnimBool etc when swapping between basic/advanced settings

            var serializedMember = GetMember(property);

            float height;
            try
            {
                var state = GetState(property);
                var properties = GetChildProperties(property);
                var metadata = DelegateMetadata.GetMetadata(serializedMember.MemberType);
                _context = new Context(properties, serializedMember, metadata, state);

                ValidateSerializedData(property);

                // Header content + body padding
                height = kHeaderHeight + 10f + Spacing;

                if (state.viewingAdvancedSettings)
                {
                    height += LineHeight + LineHeight + Spacing;

                    var behaviour = properties.editorBehaviour.intValue;
                    if (behaviour == EditorBehaviours.kReturnMockValue)
                    {
                        var mockProp = _context.properties.editorMockValue;

                        height += Spacing;

                        if (mockProp != null)
                        {
                            height += EditorGUI.GetPropertyHeight(mockProp, true);
                        }
                    }
                }
                else
                {
                    float targetHeight;
                    if (properties.targetingStaticMember.boolValue)
                    {
                        targetHeight = SerializeTypeDefinitionDrawer.GetPropertyHeight(properties.staticTargetType, false);
                    }
                    else
                    {
                        targetHeight = IsGameObjectOrComponentReference(properties.target)
                            ? LineHeight + LineHeight + Spacing
                            : LineHeight;
                    }

                    var methodAndArgsHeight = LineHeight;
                    if (!properties.argumentsDefinedByEvent.boolValue)
                    {
                        // TODO: Calc arguments height
                        methodAndArgsHeight += 0;
                    }

                    height += Spacing + Mathf.Max(targetHeight, methodAndArgsHeight);
                }

                // TODO: Arguments expanded? add height

                // Padding before next element
                height += kNextElementSpacing;
            }
            finally { _context = default; }

            // Spacing before the next element
            height += LineHeight;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var serializedMember = GetMember(property);

            position = position.PadBottom(LineHeight);

            try
            {
                var state = GetState(property);
                var properties = GetChildProperties(property);
                var metadata = DelegateMetadata.GetMetadata(serializedMember.MemberType);
                _context = new Context(properties, serializedMember, metadata, state);

                ValidateSerializedData(property);

                // This is added as padding before next element
                position.height -= kNextElementSpacing;

                var headerRect = position.WithHeight(kHeaderHeight);
                position = position.PadTop(headerRect.height + Spacing);

                using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
                {
                    DrawHeader(headerRect, label);

                    DrawBodyBackground(position);
                    position = position.Pad(left: 4, right: 4, top: 6, bottom: 4);

                    if (state.viewingAdvancedSettings)
                    {
                        ++EditorGUI.indentLevel;
                        DrawAdvancedSettings(position);
                        --EditorGUI.indentLevel;
                    }
                    else
                    {
                        var leftColumnRect = position.WithWidth(position.width * 0.38f);
                        var rightColumnRect = position.PadLeft(leftColumnRect.width + Spacing + Spacing);

                        EditorGUI.BeginChangeCheck();

                        DrawTarget(ref leftColumnRect);

                        if (EditorGUI.EndChangeCheck())
                        {
                            properties.methodName.stringValue = null;
                        }

                        // TODO: Does this need to calculate method content height or just pass by ref like DrawTarget?
                        var methodRect = rightColumnRect.WithHeight(LineHeight);

                        DrawMethod(methodRect);
                    }
                }
            }
            finally { _context = default; }
        }
    }
}
