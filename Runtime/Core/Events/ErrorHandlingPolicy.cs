namespace JakePerry.Unity.Events
{
    /// <summary>
    /// An enumeration of possible error handling policies.
    /// </summary>
    public enum ErrorHandlingPolicy
    {
        /// <summary>
        /// The default policy is used.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The error is ignored and execution is halted gracefully.
        /// </summary>
        Ignore = 1,

        /// <summary>
        /// An error is logged and execution is halted gracefully.
        /// </summary>
        LogError = 2,

        /// <summary>
        /// An exception is thrown.
        /// </summary>
        ThrowException = 3
    }
}
