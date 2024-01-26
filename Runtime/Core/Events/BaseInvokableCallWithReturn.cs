using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    // TODO: Documentation
    internal abstract class BaseInvokableCallWithReturn<TFunc>
        where TFunc : Delegate
    {
        private readonly TFunc m_func;

        protected TFunc Func => m_func;

        protected bool AllowInvoke
        {
            get
            {
                var target = m_func.Target;

                if (target is UnityEngine.Object obj)
                {
                    return obj != null;
                }

                return true;
            }
        }

        protected BaseInvokableCallWithReturn(object target, MethodInfo method)
        {
            _ = method ?? throw new ArgumentNullException(nameof(method));

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

            m_func = (TFunc)Delegate.CreateDelegate(typeof(TFunc), target, method);
        }

        internal bool Match(object target, MethodInfo method)
        {
            var func = m_func;
            return func.Target == target && func.Method.Equals(method);
        }
    }

    internal sealed class InvokableCallWithReturn<TResult> : BaseInvokableCallWithReturn<UnityFunc<TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal void Invoke()
        {
            if (AllowInvoke) Func.Invoke();
        }
    }

    internal sealed class InvokableCallWithReturn<T0, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal void Invoke(T0 arg0)
        {
            if (AllowInvoke) Func.Invoke(arg0);
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, T1, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal void Invoke(T0 arg0, T1 arg1)
        {
            if (AllowInvoke) Func.Invoke(arg0, arg1);
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, T2, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, T1, T2, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal void Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            if (AllowInvoke) Func.Invoke(arg0, arg1, arg2);
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, T2, T3, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, T1, T2, T3, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (AllowInvoke) Func.Invoke(arg0, arg1, arg2, arg3);
        }
    }
}
