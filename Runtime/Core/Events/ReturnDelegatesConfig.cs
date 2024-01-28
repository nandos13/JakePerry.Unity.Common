using UnityEngine;

namespace JakePerry.Unity.Events
{
    internal static class ReturnDelegatesConfig
    {
        private const string kErrorLoggingEnabledPrefsKey = "JakePerry.Unity.Events.ReturnDelegates.ErrorLoggingEnabled";

#if UNITYRETURNDELEGATES_DISABLE_ERROR_LOGGING
        private const bool kLoggingEnabled = false;
#else
        private const bool kLoggingEnabled = true;
#endif

        internal static bool ErrorLoggingEnabled
        {
            get
            {
#if UNITY_EDITOR
                return IsErrorLoggingEnabledInEditor();
#else
                return kLoggingEnabled;
#endif
            }
        }

        internal static TargetDestroyedErrorHandlingPolicy TargetDestroyedPolicy
        {
            get
            {
                // TODO:
                // Implement this, might need a generated ScriptableObject.
                // Also might need to have the ErrorLoggingEnabled bool in said config object too.
                // Just use a resource load for it I guess...
                return TargetDestroyedErrorHandlingPolicy.Default;
            }
        }

#if UNITY_EDITOR
        internal static bool IsErrorLoggingEnabledInEditor()
        {
            return UnityEditor.EditorPrefs.GetBool(kErrorLoggingEnabledPrefsKey, true);
        }

        internal static void SetErrorLoggingEnabledInEditor(bool enabled)
        {
            UnityEditor.EditorPrefs.SetBool(kErrorLoggingEnabledPrefsKey, enabled);
            ReturnDelegatesUtility.Log(enabled ? "Error logging enabled in Editor" : "Error logging disabled in Editor");
        }
#endif
    }
}
