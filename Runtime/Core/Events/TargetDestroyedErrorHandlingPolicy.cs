namespace JakePerry.Unity.Events
{
    /// <summary>
    /// An enumeration of possible error handling policies which come into effect
    /// when the target <see cref="UnityEngine.Object"/> of a method invocation
    /// has been destroyed.
    /// </summary>
    public enum TargetDestroyedErrorHandlingPolicy
    {
        /// <summary>
        /// The default value.
        /// <para>
        /// When a UnityReturnDelegate instance has this policy, the global
        /// policy is consulted.
        /// </para>
        /// <para>
        /// When the global policy is set to this value, <see cref="None"/>
        /// is used instead.
        /// </para>
        /// </summary>
        Default = 0,

        /// <summary>
        /// The error is ignored and invocation does not proceed.
        /// </summary>
        None = 1,

        /// <summary>
        /// An error is logged and invocation does not proceed.
        /// </summary>
        LogError = 2,

        /// <summary>
        /// An exception of type <see cref="InvocationTargetDestroyedException"/> is thrown.
        /// </summary>
        ThrowException = 3
    }
}
