using System;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    internal static class ReturnDelegatesUtility
    {
        internal static void Log(string message, UnityEngine.Object context = null)
        {
            message = $"[UnityReturnDelegates] {message}";
            Debug.Log(message, context);
        }

        internal static void LogError(string message, UnityEngine.Object context = null)
        {
            message = $"[UnityReturnDelegates] {message}";
            Debug.LogError(message, context);
        }

        internal static byte ErrorPolicyToByte(ErrorHandlingPolicy value)
        {
            if (value < ErrorHandlingPolicy.Default ||
                value > ErrorHandlingPolicy.ThrowException)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return (byte)(int)value;
        }
    }
}
