namespace JakePerry.Unity.Events
{
    /// <summary>
    /// An exception which is thrown when the target <see cref="UnityEngine.Object"/> of a
    /// method invocation is destroyed, and the error handling policy is set to
    /// <see cref="ErrorHandlingPolicy.ThrowException"/>.
    /// </summary>
    public sealed class InvocationTargetDestroyedException : InvokeFailedException
    {
        private const string kDefaultMsg = "Delegate target object is a destroyed Unity object; Invocation cannot proceed.";

        public InvocationTargetDestroyedException() : base(kDefaultMsg) { }
    }
}
