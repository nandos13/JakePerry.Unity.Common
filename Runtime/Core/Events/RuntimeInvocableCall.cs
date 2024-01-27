using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    internal abstract class RuntimeInvocableCall
    {
        private readonly MethodInfo m_method;
        private readonly object m_target;

        protected bool AllowInvoke
        {
            get
            {
                // TODO: This doesnt account for static methods, which I'd like to support...
                if (m_target is UnityEngine.Object obj)
                {
                    return obj != null;
                }

                return true;
            }
        }

        protected MethodInfo Method => m_method;

        protected object Target => m_target;

        protected RuntimeInvocableCall(object target, MethodInfo method)
        {
            m_method = method ?? throw new ArgumentNullException(nameof(method));

            if (method.IsStatic)
            {
                if (target is not null)
                {
                    throw new ArgumentException("Static method specified, target must be null.", nameof(target));
                }
            }
            else
            {
                _ = target ?? throw new ArgumentNullException(nameof(target));
            }

            m_target = target;
        }

        protected abstract object Invoke_Impl(object[] args);

        internal object Invoke(object[] args)
        {
            _ = args ?? throw new ArgumentNullException(nameof(args));
            return Invoke_Impl(args);
        }
    }
}
