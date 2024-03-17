using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    internal abstract class RuntimeInvocableCall : IInvocableCall
    {
        private readonly MethodInfo m_method;
        private readonly object m_target;

        /// <summary>
        /// Indicates whether this call is allowed to be invoked.
        /// <para>
        /// Invocation is not allowed if the target is a destroyed <see cref="UnityEngine.Object"/>.
        /// </para>
        /// </summary>
        internal bool AllowInvoke
        {
            get
            {
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

        bool IInvocableCall.AllowInvoke => this.AllowInvoke;

        object IInvocableCall.Invoke(object[] args) => this.Invoke(args);
    }
}
