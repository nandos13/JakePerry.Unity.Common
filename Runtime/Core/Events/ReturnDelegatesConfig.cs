using UnityEngine;

namespace JakePerry.Unity.Events
{
    [RuntimeSettingsPath("Project/JakePerry/Return Delegates")]
    internal sealed class ReturnDelegatesConfig : RuntimeSettingsBase
    {
        [Header("Error Policies")]

        [SerializeField]
        private bool m_errorLoggingEnabled;

        [SerializeField]
        private ErrorHandlingPolicy m_invocationFailurePolicy;

        private static ReturnDelegatesConfig Cfg => GetSettingsAndCache<ReturnDelegatesConfig>();

        internal static bool ErrorLoggingEnabled => Cfg.m_errorLoggingEnabled;

        internal static ErrorHandlingPolicy InvocationFailedPolicy =>
            Cfg.m_invocationFailurePolicy.OrFallback(ErrorHandlingPolicy.LogError);
    }
}
