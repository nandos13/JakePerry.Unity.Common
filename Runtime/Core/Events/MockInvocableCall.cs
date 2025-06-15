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
            Enforce.Argument(args, nameof(args)).IsNotNull();
            return m_result;
        }
    }
}
