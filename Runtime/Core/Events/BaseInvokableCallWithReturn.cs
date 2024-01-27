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

        internal bool Match(object target, MethodInfo method)
        {
            var func = m_func;
            return func.Target == target && func.Method.Equals(method);
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

    internal sealed class InvokableCallWithReturn<TResult> : BaseInvokableCallWithReturn<UnityFunc<TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }
        
        internal TResult Invoke()
        {
            if (AllowInvoke)
            {
                return Func.Invoke();
            }
            return default;
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 0) throw new ArgumentException("Expected array of length 0.", nameof(args));

            if (AllowInvoke)
            {
                return (object)Func.Invoke();
            }
            return null;
        }
    }

    internal sealed class InvokableCallWithReturn<T0, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0)
        {
            if (AllowInvoke)
            {
                return Func.Invoke(arg0);
            }
            return default;
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 1) throw new ArgumentException("Expected array of length 1.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);

            if (AllowInvoke)
            {
                return (object)Func.Invoke((T0)args[0]);
            }
            return null;
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, T1, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0, T1 arg1)
        {
            if (AllowInvoke)
            {
                return Func.Invoke(arg0, arg1);
            }
            return default;
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 2) throw new ArgumentException("Expected array of length 2.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);
            ThrowOnInvalidArgument<T1>(args[1], 1);

            if (AllowInvoke)
            {
                return (object)Func.Invoke((T0)args[0], (T1)args[1]);
            }
            return null;
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, T2, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, T1, T2, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            if (AllowInvoke)
            {
                return Func.Invoke(arg0, arg1, arg2);
            }
            return default;
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 3) throw new ArgumentException("Expected array of length 3.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);
            ThrowOnInvalidArgument<T1>(args[1], 1);
            ThrowOnInvalidArgument<T2>(args[2], 2);

            if (AllowInvoke)
            {
                return (object)Func.Invoke((T0)args[0], (T1)args[1], (T2)args[2]);
            }
            return null;
        }
    }

    internal sealed class InvokableCallWithReturn<T0, T1, T2, T3, TResult> : BaseInvokableCallWithReturn<UnityFunc<T0, T1, T2, T3, TResult>>
    {
        internal InvokableCallWithReturn(object target, MethodInfo method) : base(target, method) { }

        internal TResult Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (AllowInvoke)
            {
                return Func.Invoke(arg0, arg1, arg2, arg3);
            }
            return default;
        }

        protected override object Invoke_Impl(object[] args)
        {
            if (args.Length != 4) throw new ArgumentException("Expected array of length 4.", nameof(args));

            ThrowOnInvalidArgument<T0>(args[0], 0);
            ThrowOnInvalidArgument<T1>(args[1], 1);
            ThrowOnInvalidArgument<T2>(args[2], 2);
            ThrowOnInvalidArgument<T3>(args[3], 3);

            if (AllowInvoke)
            {
                return (object)Func.Invoke((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3]);
            }
            return null;
        }
    }
}
