using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    // TODO: Documentation
    internal abstract class BaseInvokableCallWithReturn<TFunc> : RuntimeInvocableCall
        where TFunc : Delegate
    {
        private readonly TFunc m_func;

        protected TFunc Func => m_func;

        protected BaseInvokableCallWithReturn(object target, MethodInfo method)
            : base(target, method)
        {
            m_func = (TFunc)Delegate.CreateDelegate(typeof(TFunc), target, method);
        }

        protected static void ThrowOnInvalidArgument<T>(object arg, int index)
        {
            if (arg is not T)
            {
                if (arg is null)
                {
                    // Ignore null for reference types
                    if (!typeof(T).IsValueType) return;

                    throw new ArgumentException($"Argument invalid at index {index}; Expected a value-type argument of type {typeof(T)} but the passed value is null.");
                }

                throw new ArgumentException($"Argument invalid at index {index}; Expected argument of type {typeof(T)}, passed value of type {arg.GetType()}.");
            }
        }
    }

    internal sealed class InvokableCallWithReturn<TResult> : BaseInvokableCallWithReturn<Func<TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke()
        {
            if (!PreInvoke(out var ex)) throw ex;
            return Func.Invoke();
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 0) throw new ArgumentException("Expected array of length 0.", nameof(args));

            return (object)Func.Invoke();
        }
    }

    internal sealed class InvokableCallWithReturn<T0, TResult> : BaseInvokableCallWithReturn<Func<T0, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0)
        {
            if (!PreInvoke(out var ex)) throw ex;
            return Func.Invoke(arg0);
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 1) throw new ArgumentException("Expected array of length 1.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);

            return (object)Func.Invoke((T0)args[0]);
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, TResult> : BaseInvokableCallWithReturn<Func<T0, T1, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0, T1 arg1)
        {
            if (!PreInvoke(out var ex)) throw ex;
            return Func.Invoke(arg0, arg1);
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 2) throw new ArgumentException("Expected array of length 2.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);
            ThrowOnInvalidArgument<T1>(args[1], 1);

            return (object)Func.Invoke((T0)args[0], (T1)args[1]);
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, T2, TResult> : BaseInvokableCallWithReturn<Func<T0, T1, T2, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            if (!PreInvoke(out var ex)) throw ex;
            return Func.Invoke(arg0, arg1, arg2);
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 3) throw new ArgumentException("Expected array of length 3.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);
            ThrowOnInvalidArgument<T1>(args[1], 1);
            ThrowOnInvalidArgument<T2>(args[2], 2);

            return (object)Func.Invoke((T0)args[0], (T1)args[1], (T2)args[2]);
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, T2, T3, TResult> : BaseInvokableCallWithReturn<Func<T0, T1, T2, T3, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!PreInvoke(out var ex)) throw ex;
            return Func.Invoke(arg0, arg1, arg2, arg3);
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 4) throw new ArgumentException("Expected array of length 4.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);
            ThrowOnInvalidArgument<T1>(args[1], 1);
            ThrowOnInvalidArgument<T2>(args[2], 2);
            ThrowOnInvalidArgument<T3>(args[3], 3);

            return (object)Func.Invoke((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3]);
        }
    }
}
