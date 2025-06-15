using JakePerry.Debugging;
using System;
using UnityEngine;

using StackTrace = System.Diagnostics.StackTrace;

namespace JakePerry.Unity.Debugging
{
    /// <summary>
    /// Implementation of <see cref="IDebugImpl"/> using Unity's <see cref="Debug"/> class.
    /// </summary>
    internal sealed class UnityDebugImpl : IDebugImpl
    {
        private static readonly UnityDebugImpl _inst = new();

        private static void HandleLog(bool trace, bool error, string message)
        {
            var logType = error ? LogType.Error : LogType.Log;
            var logOption = trace ? LogOption.None : LogOption.NoStacktrace;

            Debug.LogFormat(logType, logOption, null, "{0}", args: new object[] { message });
        }

        void IDebugImpl.Assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        void IDebugImpl.LogError(bool trace, string message)
        {
            HandleLog(trace, true, message);
        }

        void IDebugImpl.LogError(StackTrace trace, string message)
        {
            var stackTrace = trace?.ToString();
            if (stackTrace is not null)
            {
                message = string.Concat(message, ",\n", stackTrace);
            }
            HandleLog(false, true, message);
        }

        void IDebugImpl.LogInfo(bool trace, string message)
        {
            HandleLog(trace, false, message);
        }

        void IDebugImpl.LogException(Exception exception)
        {
            Debug.LogException(exception);
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            JPDebug.SetImplementation(_inst);
        }
    }
}
