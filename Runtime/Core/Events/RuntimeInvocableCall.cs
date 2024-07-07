using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    // TODO: Documentation
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

        /// <summary>
        /// Performs the necessary validation required before the runtime call is invoked.
        /// </summary>
        /// <param name="ex">
        /// Out parameter which is assigned an exception that must be thrown if
        /// this method returns <see langword="false"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the invocation is allowed to proceed; Otherwise,
        /// <see langword="false"/> if <paramref name="ex"/> is to be thrown.
        /// </returns>
        /// <remarks>
        /// This method returns an exception and expects the caller to throw it
        /// to improve stacktrace readability.
        /// </remarks>
        protected bool PreInvoke(out Exception ex)
        {
            ex = null;
            if (m_target is UnityEngine.Object obj && obj == null)
            {
                ex = new InvocationTargetDestroyedException();
            }

            return ex is null;
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

            if (!PreInvoke(out var ex)) throw ex;

            return Invoke_Impl(args);
        }

        object IInvocableCall.Invoke(object[] args) => this.Invoke(args);
    }
}
