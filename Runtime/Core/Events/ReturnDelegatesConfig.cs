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
        private ErrorHandlingPolicy m_methodResolutionFailurePolicy;

        [SerializeField]
        private ErrorHandlingPolicy m_invocationFailurePolicy;

        private static ReturnDelegatesConfig Cfg => GetSettingsAndCache<ReturnDelegatesConfig>();

        private static ErrorHandlingPolicy GetPolicy(ErrorHandlingPolicy o, ErrorHandlingPolicy @default)
        {
            if (o == ErrorHandlingPolicy.Default) return @default;
            return o;
        }

        internal static bool ErrorLoggingEnabled => Cfg.m_errorLoggingEnabled;

        internal static ErrorHandlingPolicy InvocationFailedPolicy =>
            GetPolicy(Cfg.m_invocationFailurePolicy, ErrorHandlingPolicy.LogError);

        internal static ErrorHandlingPolicy FailedToResolveMethodPolicy =>
            GetPolicy(Cfg.m_methodResolutionFailurePolicy, ErrorHandlingPolicy.LogError);
    }
}
