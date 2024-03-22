using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;
using static JakePerry.Unity.Events.ReturnDelegatesEditorUtil;

namespace JakePerry.Unity.Events
{
    [CustomPropertyDrawer(typeof(UnityReturnDelegateBase), useForChildren: true)]
    public sealed class UnityReturnDelegateDrawer : PropertyDrawer
    {
        private const string kPolicyTooltip = "Policy used when the target invocation object is destroyed.";
        private const string kPolicyTooltipStatic = "* Not applicable for static member delegates. *\n" + kPolicyTooltip;
        private const string kMockingNotSerializableMessage = "Return type is not serializable. Default value will be used.";

        private const string kBasicSettingsTabHint = "UnityReturnDelegateDrawer.Tab.Basic";
        private const string kAdvancedSettingsTabHint = "UnityReturnDelegateDrawer.Tab.Advanced";

        private const float kHeaderHeight = 18f;
        private const float kNextElementSpacing = 1f;

        private sealed class State
        {
            public bool viewingAdvancedSettings;
        }

        private sealed class AssignMethodArguments
        {
            public SerializedProperty property;
            public MemberInfo member;
            public bool dynamicArguments;
        }

        private sealed class PropertyCache
        {
            public readonly SerializedProperty property;

            public readonly SerializedProperty target;
            public readonly SerializedProperty staticTargetType;
            public readonly SerializedProperty targetingStaticMember;
            public readonly SerializedProperty methodName;
            public readonly SerializedProperty argumentsDefinedByEvent;
            public readonly SerializedProperty policy;
            public readonly SerializedProperty editorBehaviour;
            public readonly SerializedProperty editorMockValue;

            public PropertyCache(SerializedProperty property)
            {
                this.property = property;

                target = property.FindPropertyRelative("m_target");
                staticTargetType = property.FindPropertyRelative("m_staticTargetType");
                targetingStaticMember = property.FindPropertyRelative("m_targetingStaticMember");
                methodName = property.FindPropertyRelative("m_methodName");
                argumentsDefinedByEvent = property.FindPropertyRelative("m_argumentsDefinedByEvent");

                policy = property.FindPropertyRelative("m_policy");
                editorBehaviour = property.FindPropertyRelative("m_editorBehaviour");
                editorMockValue = property.FindPropertyRelative("m_editorMockValue");
            }
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
        private static readonly List<AssignMethodArguments> _assignArgsCache = new(capacity: 64);

        private static Context _context;
        private static int _assignOptionIndex;

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void OnRecompile()
        {
            _propertyCache.Clear();
            _memberCache.Clear();
            _stateCache.Clear();
            _assignArgsCache.Clear();
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

        private static void DrawHintIcon(Rect rect, string tooltip)
        {
            var icon = UnityEditorHelper.GetMessageIcon(MessageType.Error);
            var iconStyle = EditorStyles.iconButton;
            rect = iconStyle.margin.Remove(rect);

            var hintContent = GetTempContent(icon);
            hintContent.tooltip = tooltip;

            EditorGUI.LabelField(rect, hintContent, iconStyle);
        }

        private static void AssignMethod(object e)
        {
            var args = (AssignMethodArguments)e;

            // TODO: Assign, validate arguments, etc.
            // member could be a method or property.
        }

        private static void AddMemberSelectOption(
            GenericMenu menu,
            string name,
            bool on,
            ref int index,
            SerializedProperty property,
            MemberInfo member,
            bool dynamicArguments)
        {
            var args = index >= _assignArgsCache.Count
                ? new AssignMethodArguments()
                : _assignArgsCache[index];

            ++index;

            args.property = property;
            args.member = member;
            args.dynamicArguments = dynamicArguments;

            menu.AddItem(new GUIContent(name), on, _assignMethodCallback, args);
        }

        private void DrawHeader(Rect rect, GUIContent label)
        {
            // TODO: Consider supporting argument coloring for header signature
            // TODO: Consider showing small error icon on left of name if any errors are present.
            //       Will need to draw header last in that case

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

            var policyProp = _context.properties.policy;
            var behaviourProp = _context.properties.editorBehaviour;
            var modeProp = _context.properties.targetingStaticMember;

            var policy = policyProp.intValue;
            var behaviour = behaviourProp.intValue;
            bool @static = modeProp.boolValue;

            var policyRect = rect.WithHeight(LineHeight);
            rect = rect.PadTop(LineHeight + Spacing);

            var behaviourRect = rect.WithHeight(LineHeight);
            rect = rect.PadTop(LineHeight + Spacing);

            var labelContent = GetTempContent(
                text: "Destroyed Target Policy",
                tooltip: @static ? kPolicyTooltipStatic : kPolicyTooltip);
            policyRect = EditorGUI.PrefixLabel(policyRect, labelContent);

            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(@static))
            {
                policy = EditorGUI.Popup(policyRect, policy, PolicyOptions);
            }

            if (EditorGUI.EndChangeCheck()) policyProp.intValue = policy;

            behaviourRect = EditorGUI.PrefixLabel(behaviourRect, GetTempContent("Editor Behaviour"));

            EditorGUI.BeginChangeCheck();
            behaviour = EditorGUI.Popup(behaviourRect, behaviour, EditorInvocationOptions);

            if (EditorGUI.EndChangeCheck()) behaviourProp.intValue = behaviour;

            if (behaviour == UnityReturnDelegateBase.EditorBehaviours.kReturnMockValue)
            {
                Rect mockValueRect;
                var mockProp = _context.properties.editorMockValue;
                if (mockProp == null)
                {
                    mockValueRect = rect.WithHeight(LineHeight);
                    EditorGUI.HelpBox(mockValueRect, kMockingNotSerializableMessage, MessageType.Warning);
                }
                else
                {
                    mockValueRect = rect.WithHeight(EditorGUI.GetPropertyHeight(mockProp, true));

                    // TODO: Check this when nested, make sure indent is correct.
                    mockValueRect = EditorGUI.PrefixLabel(mockValueRect, GetTempContent("Mock Value"));

                    EditorGUI.PropertyField(mockValueRect, mockProp, GUIContent.none, true);
                }

                rect = rect.PadTop(mockValueRect.height + Spacing);
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

        private void DrawTarget(ref Rect rect)
        {
            var property = _context.properties.property;

            var modeProp = _context.properties.targetingStaticMember;
            var staticTargetProp = _context.properties.staticTargetType;
            var targetProp = _context.properties.target;

            bool @static = modeProp.boolValue;

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

        private static string GetNiceMemberString(MemberInfo member, bool includeReturnType)
        {
            if (member is PropertyInfo p)
            {
                return GetNicePropertyString(p, includeReturnType, PropertyMethodType.Get);
            }

            return GetNiceMethodString(member as MethodInfo, includeReturnType);
        }

        private GenericMenu BuildStaticMemberPopupList(Type declaringType, MethodInfo currentMethod)
        {
            var metadata = _context.metadata;
            var property = _context.properties.property;
            var definedByEvent = _context.properties.argumentsDefinedByEvent.boolValue;

            // TODO: Idea, Invocation Argument base class is used for all the
            // serializable arguments. If there was a system that could bind
            // these types, the system could be extended to support methods
            // that have more parameter types than just the unity-supported ones!
            // Or... All reference types are supported out of the box.
            // Structs can be bound by user as required...

            var menu = new GenericMenu();

            AddMemberSelectOption(menu, "None", currentMethod is null, ref _assignOptionIndex, property, null, true);

            var list = GetMembersWithReturnType(declaringType, metadata.returnType);

            list.RemoveAll(m =>
            {
                bool isStatic = (m is PropertyInfo p)
                    ? p.GetGetMethod().IsStatic
                    : (m as MethodInfo).IsStatic;

                return !isStatic;
            });
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
                    AddMemberSelectOption(menu, GetNiceMemberString(m, false), on, ref _assignOptionIndex, property, m, true);
                }
            }

            list2.Clear();

            bool anyReturnsSubclass = false;
            foreach (var m in list)
            {
                // TODO: Match static args. Just anything thats actually serializable as InvocationArgument.

                if (!anyReturnsSubclass)
                {
                    anyReturnsSubclass = ((m is PropertyInfo p) ? p.PropertyType : (m as MethodInfo).ReturnType) != metadata.returnType;
                }

                list2.Add(m);
            }

            if (list2.Count > 0)
            {
                bool includeReturnType = anyReturnsSubclass;

                menu.AddSeparator(string.Empty);

                menu.AddItem(new GUIContent("Static Arguments"), false, null);

                foreach (var m in list2)
                {
                    bool on = currentMethod == m && !definedByEvent;
                    AddMemberSelectOption(menu, GetNiceMemberString(m, includeReturnType), on, ref _assignOptionIndex, property, m, false);
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

                        currentMethod = UnityReturnDelegateBase.GetValidMethodInfo(invocationType, @static, methodNameProp.stringValue, metadata.returnType, currentArgumentTypes, out methodResolveError);

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
                    if (@static)
                    {
                        Debug.Assert(invocationTarget is Type);
                        BuildStaticMemberPopupList((Type)invocationTarget, currentMethod).DropDown(rect);
                    }
                    else
                    {
                        Debug.Assert(invocationTarget is UnityEngine.Object);
                        BuildInstanceMemberPopupList((UnityEngine.Object)invocationTarget);
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // TODO: Use an AnimBool etc when swapping between basic/advanced settings

            var state = GetState(property);
            var properties = GetChildProperties(property);

            // Header content + body padding
            float height = kHeaderHeight + 10f + Spacing;

            if (state.viewingAdvancedSettings)
            {
                height += LineHeight + LineHeight + Spacing;
                // TODO: Args height

                var behaviour = properties.editorBehaviour.intValue;
                if (behaviour == UnityReturnDelegateBase.EditorBehaviours.kReturnMockValue)
                {
                    var mockProp = properties.editorMockValue;

                    height += Spacing;
                    height += mockProp != null
                        ? EditorGUI.GetPropertyHeight(mockProp, GUIContent.none, true)
                        : LineHeight;
                }
            }
            else
            {
                float targetHeight = properties.targetingStaticMember.boolValue
                    ? SerializeTypeDefinitionDrawer.GetPropertyHeight(properties.staticTargetType, false)
                    : LineHeight;

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

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _assignOptionIndex = 0;

            var serializedMember = GetMember(property);

            try
            {
                var state = GetState(property);
                var metadata = DelegateMetadata.GetMetadata(serializedMember.MemberType);
                _context = new Context(GetChildProperties(property), serializedMember, metadata, state);

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

                        DrawTarget(ref leftColumnRect);

                        if (EditorGUI.EndChangeCheck())
                        {
                            _context.properties.methodName.stringValue = null;
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
