using System;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// An exception which is thrown when the target <see cref="UnityEngine.Object"/> of a
    /// method invocation is destroyed, and the error handling policy is set to
    /// <see cref="TargetDestroyedErrorHandlingPolicy.ThrowException"/>.
    /// </summary>
    public sealed class InvocationTargetDestroyedException : Exception { }
}
