using System;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// An exception which occurs when a delegate invocation fails.
    /// </summary>
    public class InvokeFailedException : JpBaseException
    {
        private const string kUnknownErrMsg = "An unknown error occurred during delegate invocation.";
        private const string kSeeInnerMsg = "An error occurred during delegate invocation. See inner exception for details.";

        public InvokeFailedException() : base(kUnknownErrMsg) { }

        public InvokeFailedException(string message) : base(message) { }

        public InvokeFailedException(string message, Exception inner) : base(message, inner) { }

        public InvokeFailedException(Exception inner) : base(kSeeInnerMsg, inner) { }
    }
}
