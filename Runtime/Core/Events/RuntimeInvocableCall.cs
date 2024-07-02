using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    internal abstract class RuntimeInvocableCall : IInvocableCall
    {
        private readonly MethodInfo m_method;
        private readonly object m_target;

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

        /// <summary>
        /// Invoke the call with the given arguments.
        /// </summary>
        /// <param name="args">
        /// Invocation arguments.
        /// </param>
        /// <returns>
        /// Object instance returned by the invocation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="args"/> is <langword cref="null"/>.
        /// </exception>
        /// <exception cref="InvocationTargetDestroyedException">
        /// The invocation target is a destroyed <see cref="UnityEngine.Object"/>.
        /// </exception>
        internal object Invoke(object[] args)
        {
            _ = args ?? throw new ArgumentNullException(nameof(args));

            if (m_target is UnityEngine.Object obj && obj == null)
            {
                throw new InvocationTargetDestroyedException();
            }

            return Invoke_Impl(args);
        }

        object IInvocableCall.Invoke(object[] args) => this.Invoke(args);
    }
}
