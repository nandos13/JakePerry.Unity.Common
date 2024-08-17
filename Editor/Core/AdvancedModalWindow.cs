using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JakePerry.Unity
{
    public sealed class AdvancedModalWindow : EditorWindow
    {
        private VisualElement m_clickOutPanel;
        private VisualElement m_contentRoot;

        private void OnGUI()
        {
            //GUILayout.BeginArea(rr);
            //var e = Event.current;
            //if (e.type != EventType.Layout && e.type != EventType.Repaint)
            //    Debug.LogError($"[{System.Threading.Thread.CurrentThread.ManagedThreadId}] {e.type}");

            EditorGUILayout.LabelField("Test");
            if (GUILayout.Button("Close"))
                Close();

            //GUILayout.EndArea();
        }

        private void CreateGUI()
        {
            var panelRoot = rootVisualElement.panel.visualTree;
            m_clickOutPanel = panelRoot;

            var newRoot = new VisualElement();
            panelRoot.Add(newRoot);

            newRoot.Add(rootVisualElement);
            newRoot.Add(panelRoot.Q<IMGUIContainer>());

            newRoot.style.borderTopColor = Color.black;
            newRoot.style.borderBottomColor = Color.black;
            newRoot.style.borderLeftColor = Color.black;
            newRoot.style.borderRightColor = Color.black;
            newRoot.style.borderTopWidth = 1;
            newRoot.style.borderBottomWidth = 1;
            newRoot.style.borderLeftWidth = 1;
            newRoot.style.borderRightWidth = 1;

            m_contentRoot = newRoot;

            panelRoot.style.backgroundColor = new Color(1, 0, 0, 0.5f);


            //var root = rootVisualElement;

            //m_root = root.panel.visualTree;
            //m_imguiRoot = m_root.Q<IMGUIContainer>();
            //PrintHierarchy(m_root);
            //m_uxmlRoot = ;
        }

        private static void PrintHierarchy(VisualElement root, StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; ++i)
                sb.Append("--");

            sb.Append(' ');
            sb.Append(root.name);
            sb.Append(" (type: ");
            sb.Append(root.GetType().Name);
            sb.Append(')');
            sb.AppendLine();

            foreach (var c in root.Children())
            {
                PrintHierarchy(c, sb, indent + 1);
            }
        }

        private static void PrintHierarchy(VisualElement root)
        {
            var sb = new StringBuilder();

            PrintHierarchy(root, sb, 0);

            Debug.LogError(sb.ToString());
        }

        private static void MakeModal(EditorWindow window)
        {
            const BindingFlags kFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            var method = ReflectionEx.GetMethod(typeof(EditorWindow), "MakeModal", kFlags);

            Debug.LogError($"Before MakeModal");
            method.Invoke(window, Array.Empty<object>());
            Debug.LogError($"After MakeModal");
        }

        private static void Something(Rect position)
        {
            //position.position = GUIUtility.GUIToScreenPoint(position.position);

            var window = CreateInstance<AdvancedModalWindow>();

            var display = Screen.mainWindowDisplayInfo;
            var screenPos = Screen.mainWindowPosition;
            //var editorScreenType = ReflectionEx.GetType(typeof(Screen).Assembly, "UnityEngine.EditorScreen");
            //var editorWidthProp = ReflectionEx.GetProperty(editorScreenType, "width", BindingFlags.Static | BindingFlags.Public);
            //var editorHeightProp = ReflectionEx.GetProperty(editorScreenType, "height", BindingFlags.Static | BindingFlags.Public);
            //var editorW = (int)editorWidthProp.GetValue(null);
            //var editorH = (int)editorHeightProp.GetValue(null);
            var screenRect = new Rect(0, 0, display.width, display.height).PadLeft(screenPos.x).PadTop(screenPos.y);
            var guiRect = GUIUtility.ScreenToGUIRect(screenRect);
            //Debug.LogError($"screenPos: {screenPos}, res {editorW}x{editorH}");
            //Debug.LogError($"guiRect: {guiRect}");

            window.ShowAsDropDown(guiRect, guiRect.size);

            window.maxSize = screenRect.size;
            window.position = new Rect(guiRect.position, guiRect.size);


            //foo.Invoke(window, System.Array.Empty<object>());

            //window.wantsMouseEnterLeaveWindow = true;

            //window.ShowModal();
            //window.ShowModalUtility();

            //EditorApplication.update -= window.Foo;
            //EditorApplication.update += window.Foo;

            var clickPanel = window.m_clickOutPanel;
            clickPanel.RegisterCallback<MouseDownEvent>((evt) =>
            {
                Debug.LogError($"panel receive MouseDown");
                window.GiveFocusToEditor();
            });

            clickPanel.RegisterCallback<MouseUpEvent>(e =>
            {
                Debug.LogError("panel receive MouseUp");
            });

            //panel.RegisterCallback<ClickEvent>(e =>
            //{
            //    Debug.LogError($"Panel receive click");
            //});

            /*
            clickPanel.RegisterCallback<MouseEnterEvent>(e =>
            {
                Debug.LogError("panel receive MouseEnterEvent");
                clickPanel.ReleaseMouse();
            });

            clickPanel.RegisterCallback<MouseLeaveEvent>(e =>
            {
                Debug.LogError("panel receive MouseLeaveEvent ");
                clickPanel.CaptureMouse();
            });
            */

            clickPanel.RegisterCallback<MouseCaptureOutEvent>(e =>
            {
                Debug.LogError("panel receive MouseCaptureOutEvent");
                //window.Close();
            });

            Debug.LogError($"Dropdown rect : {window.rootVisualElement.contentRect}");
            Debug.LogError($"Panel rect : {clickPanel.contentRect}");

            var contentContainer = window.m_contentRoot;
            contentContainer.style.position = Position.Absolute;
            contentContainer.style.width = 200;
            contentContainer.style.height = 100;
            contentContainer.style.translate = new Translate(new Length(position.x, LengthUnit.Pixel), new Length(position.y, LengthUnit.Pixel));

            var invisibleElement = new VisualElement();
            //invisibleElement.visible = false;
            clickPanel.Add(invisibleElement);
            invisibleElement.SendToBack();
            //invisibleElement.BringToFront();

            invisibleElement.style.position = Position.Absolute;
            invisibleElement.style.visibility = Visibility.Visible;
            invisibleElement.style.width = guiRect.width;
            invisibleElement.style.height = guiRect.height;

            //invisibleElement.StretchToParentSize();

            MakeModal(window);
        }

        private static void GetEditorScreen(out float w, out float h)
        {
            var editorScreen = ReflectionEx.GetType(typeof(Screen).Assembly, "UnityEngine.EditorScreen");
            var editorWidthProp = ReflectionEx.GetProperty(editorScreen, "width", BindingFlags.Static | BindingFlags.Public);
            var editorHeightProp = ReflectionEx.GetProperty(editorScreen, "height", BindingFlags.Static | BindingFlags.Public);
            w = (int)editorWidthProp.GetValue(null);
            h = (int)editorHeightProp.GetValue(null);
        }

        public static void ShowModalDropdown(Rect position)
        {
            var guiClip = ReflectionEx.GetType(typeof(GUI).Assembly, "UnityEngine.GUIClip");

            var topmostRect = ReflectionEx.GetProperty(guiClip, "topmostRect",
                BindingFlags.Static | BindingFlags.NonPublic);
            var visibleRect = ReflectionEx.GetProperty(guiClip, "visibleRect",
                BindingFlags.Static | BindingFlags.NonPublic);

            //Debug.LogError($"topmostRect: {topmostRect.GetValue(null)}");
            Debug.LogError($"visibleRect: {visibleRect.GetValue(null)}");

            var GetTopRect = ReflectionEx.GetMethod(guiClip, "GetTopRect",
                BindingFlags.Static | BindingFlags.NonPublic);

            //Debug.LogError($"GetTopRect: {(Rect)GetTopRect.Invoke(null, Array.Empty<object>())}");

            var clipToWindow = ReflectionEx.GetMethod(guiClip, "UnclipToWindow",
                BindingFlags.Static | BindingFlags.Public,
                new ParamsArray<Type>(typeof(Vector2)));

            var absMousePos = ReflectionEx.GetMethod(guiClip, "GetAbsoluteMousePosition",
                BindingFlags.Static | BindingFlags.Public);

            var topRect = (Vector2)clipToWindow.Invoke(null, new object[] { new Vector2(0, 0) });
            Debug.LogError("Top rect: " + topRect);

            var mPos = absMousePos.Invoke(null, Array.Empty<object>());
            Debug.LogError($"GetAbsoluteMousePosition: {mPos}, clipped: {clipToWindow.Invoke(null, new object[] { mPos })}");

            var display = Screen.mainWindowDisplayInfo;
            var screenPos = Screen.mainWindowPosition;
            
            var screenRect2 = new Rect(0, 0, display.width, display.height).PadLeft(screenPos.x).PadTop(screenPos.y);
            var guiRect = GUIUtility.ScreenToGUIRect(screenRect2);
            Debug.LogError($"screenPos: {screenPos}");
            Debug.LogError($"guiRect: {guiRect}");

            var screenRect = GUIUtility.GUIToScreenRect(position);
            EditorApplication.delayCall += () =>
            {
                Something(screenRect);
            };
        }

        public void GiveFocusToEditor()
        {
            rootVisualElement.Focus();
            //Debug.LogError($"GiveFocusToEditor focused: {rootVisualElement.focusController.focusedElement}");
        }

        private void OnClickEvent(ClickEvent e)
        {
            //Debug.LogError("OnClickEvent");
        }

        private void OnFocus()
        {
            //Debug.LogError($"OnFocus");
            rootVisualElement.Focus();
        }

        private void OnLostFocus()
        {
            //Debug.LogError($"OnLostFocus");
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            Debug.LogError($"OnKeyDown {e.keyCode}");
            e.StopImmediatePropagation();

            if (e.keyCode == KeyCode.Escape)
            {
                Close();
            }
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            Debug.LogError("OnMouseDown");
        }

        private void OnEnable()
        {
            var element = rootVisualElement;

            //Debug.LogError("OnEnable");
            element.focusable = true;
            element.RegisterCallback<ClickEvent>(OnClickEvent);
            element.RegisterCallback<KeyDownEvent>(OnKeyDown);
            element.RegisterCallback<MouseDownEvent>(OnMouseDown);
            //Debug.LogError("OnEnable finished...");
        }

        private void OnDisable()
        {
            //Debug.LogError("OnDisable");
        }

        private void OnDestroy()
        {
            //EditorApplication.update -= Foo;
            //Debug.LogError("OnDestroy");
        }
    }
}
