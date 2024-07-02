using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

using static JakePerry.Unity.EditorHelpersStatic;

namespace JakePerry.Unity
{
    [RequiresConstantRepaint(If = nameof(IsAnimating))]
    [CustomPropertyDrawer(typeof(PropertyPath))]
    public sealed class PropertyPathDrawer : PropertyDrawer
    {
        private const string kShowErrorPolicyPrefKey = "JakePerry.Unity.PropertyPath.ShowErrorPolicy";

        private static readonly AnimBool _showErrorPolicy = new();

        [InitializeOnLoadMethod]
        private static void Init()
        {
            _showErrorPolicy.value = EditorPrefs.GetBool(kShowErrorPolicyPrefKey, false);
        }

        private bool IsAnimating()
        {
            return _showErrorPolicy.isAnimating;
        }

        private static void ToggleShowErrorPolicy()
        {
            bool showErrorPolicy = _showErrorPolicy.target;
            showErrorPolicy = !showErrorPolicy;

            EditorPrefs.SetBool(kShowErrorPolicyPrefKey, showErrorPolicy);
            _showErrorPolicy.target = showErrorPolicy;
        }

        private void ShowContextMenu()
        {
            var menu = new GenericMenu();

            bool showingErrorPolicy = _showErrorPolicy.target;
            menu.AddItem(new GUIContent("Show error handling policy"), showingErrorPolicy, ToggleShowErrorPolicy);

            menu.ShowAsContext();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = LineHeight;
            if (property.isExpanded)
            {
                height += LineHeight + Spacing;

                height += (Spacing + LineHeight) * _showErrorPolicy.faded;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var lineRect = position.WithHeight(LineHeight);
            position = position.PadTop(lineRect.height + Spacing);

            property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded, label, true);

            // TODO: Validate step, then check if its valid or not.
            if (true)
            {
                var summaryRect = lineRect.WithWidth(lineRect.height, anchorRight: true);
                var summary = GetTempContent(UnityEditorHelper.GetMessageIcon(MessageType.Warning), tooltip: "Invalid");

                EditorGUI.LabelField(summaryRect, summary, EditorGUIEx.Styles.LabelRightAlign);
            }

            if (!property.isExpanded) return;

            using var indentScope = new EditorGUI.IndentLevelScope(1);

            lineRect = position.WithHeight(LineHeight);
            position = position.PadTop(lineRect.height + Spacing);

            var optionsRect = lineRect.WithWidth(LineHeight, anchorRight: true);
            lineRect.width -= optionsRect.width + Spacing;

            if (EditorGUIEx.ThreeDotMenuButton(optionsRect))
            {
                ShowContextMenu();
            }

            var rootObjProp = property.FindPropertyRelative("m_rootObject");
            EditorGUI.PropertyField(lineRect, rootObjProp);

            if (_showErrorPolicy.isAnimating || _showErrorPolicy.value)
            {
                lineRect = position.WithHeight(LineHeight * _showErrorPolicy.faded);
                position = position.PadTop(lineRect.height + Spacing);

                // TODO: Label width was wrong, there's probably more that breaks too. Test this thoroughly with
                // various levels of nesting, etc.
                var labelWidth = EditorGUIUtility.labelWidth;
                GUI.BeginClip(lineRect);

                EditorGUIUtility.labelWidth = labelWidth;
                lineRect.x = lineRect.y = 0;

                var policyProp = property.FindPropertyRelative("m_errorPolicy");
                EditorGUI.PropertyField(lineRect, policyProp);

                GUI.EndClip();
            }

            // TODO: Implement the bulk of this class. Also want to support statics.
            // I think this class will work very similar to the return delegates. See what can be done
            // to share some code between the two systems.
        }
    }
}
