using UnityEngine;

namespace JakePerry.Unity.Events
{
    [RuntimeSettingsPath("Project/JakePerry/Return Delegates")]
    internal sealed class ReturnDelegatesConfig : RuntimeSettingsBase
    {
        [SerializeField]
        private bool m_errorLoggingEnabled;

        [SerializeField]
        private ErrorHandlingPolicy m_targetDestroyedPolicy;

        [SerializeField]
        private ErrorHandlingPolicy m_failToResolveMethodPolicy;

        private static ReturnDelegatesConfig Cfg => GetSettingsAndCache<ReturnDelegatesConfig>();

        private static ErrorHandlingPolicy GetPolicy(ErrorHandlingPolicy o, ErrorHandlingPolicy @default)
        {
            if (o == ErrorHandlingPolicy.Default) return @default;
            return o;
        }

        internal static bool ErrorLoggingEnabled => Cfg.m_errorLoggingEnabled;

        internal static ErrorHandlingPolicy TargetDestroyedPolicy =>
            GetPolicy(Cfg.m_targetDestroyedPolicy, ErrorHandlingPolicy.LogError);

        internal static ErrorHandlingPolicy FailedToResolveMethodPolicy =>
            GetPolicy(Cfg.m_failToResolveMethodPolicy, ErrorHandlingPolicy.LogError);
    }
}
