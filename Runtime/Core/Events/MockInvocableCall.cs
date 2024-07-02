using System;

namespace JakePerry.Unity.Events
{
    internal sealed class MockInvocableCall<TResult> : IInvocableCall
    {
        private readonly TResult m_result;

        internal TResult MockResult => m_result;

        internal MockInvocableCall(TResult value)
        {
            m_result = value;
        }

        object IInvocableCall.Invoke(object[] args)
        {
            _ = args ?? throw new ArgumentNullException(nameof(args));
            return m_result;
        }
    }
}
