namespace JakePerry.Unity.Events
{
    /// <summary>
    /// An exception which is thrown when the invocation method is unable
    /// to be resolved, and the error handling policy is set to
    /// <see cref="ErrorHandlingPolicy.ThrowException"/>.
    /// </summary>
    public sealed class ResolveMethodFailedException : JpBaseException
    {
        public ResolveMethodFailedException(string message) : base(message) { }
    }
}
