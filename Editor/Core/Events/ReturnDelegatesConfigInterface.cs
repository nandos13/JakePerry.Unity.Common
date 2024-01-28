using UnityEditor;

namespace JakePerry.Unity.Events
{
    internal static class ReturnDelegatesConfigInterface
    {
        private const string kEnableErrorLoggingContextPath = Project.kContextMenuItemsPath + "Return Delegates/Enable Error Logs";

        private static void UpdateErrorLoggingStateInMenu()
        {
            bool enabled = ReturnDelegatesConfig.IsErrorLoggingEnabledInEditor();
            Menu.SetChecked(kEnableErrorLoggingContextPath, enabled);
        }

        [MenuItem(kEnableErrorLoggingContextPath)]
        public static void ToggleErrorLoggingEnabled()
        {
            bool enabled = ReturnDelegatesConfig.IsErrorLoggingEnabledInEditor();
            ReturnDelegatesConfig.SetErrorLoggingEnabledInEditor(!enabled);

            UpdateErrorLoggingStateInMenu();
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            UpdateErrorLoggingStateInMenu();
        }
    }
}
