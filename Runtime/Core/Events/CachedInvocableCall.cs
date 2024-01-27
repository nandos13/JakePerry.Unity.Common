using System.Reflection;

namespace JakePerry.Unity.Events
{
    internal sealed class CachedInvocableCall : RuntimeInvocableCall
    {
        private readonly object[] m_arguments;

        internal CachedInvocableCall(object target, MethodInfo method, object[] arguments)
            : base(target, method)
        {
            m_arguments = arguments;
        }

        internal object Invoke()
        {
            if (AllowInvoke)
            {
                return Method.Invoke(Target, m_arguments);
            }
            return null;
        }

        protected override object Invoke_Impl(object[] args)
        {
            /* Implementation note: We don't care about the arguments that were passed with invocation,
             * as this class explicitly caches the values that we should invoke with.
             */

            return Invoke();
        }
    }
}
