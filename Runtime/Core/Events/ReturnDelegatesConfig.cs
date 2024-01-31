using UnityEngine;

namespace JakePerry.Unity.Events
{
    [RuntimeSettingsPath("Project/JakePerry/Return Delegates")]
    internal sealed class ReturnDelegatesConfig : RuntimeSettingsBase
    {
        [SerializeField]
        private bool m_errorLoggingEnabled;

        [SerializeField]
        private TargetDestroyedErrorHandlingPolicy m_targetDestroyedPolicy;

        private static ReturnDelegatesConfig Cfg => GetSettingsAndCache<ReturnDelegatesConfig>();

        internal static bool ErrorLoggingEnabled => Cfg.m_errorLoggingEnabled;

        internal static TargetDestroyedErrorHandlingPolicy TargetDestroyedPolicy => Cfg.m_targetDestroyedPolicy;
    }
}
