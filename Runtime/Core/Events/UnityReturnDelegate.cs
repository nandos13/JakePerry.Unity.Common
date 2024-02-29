using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    [Serializable]
    public class UnityReturnDelegate<TResult> : UnityReturnDelegateBase
    {
        protected sealed override Type ReturnType => typeof(TResult);

        internal override RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method)
        {
            return new InvokableCallWithReturn<TResult>(target, method);
        }

        internal protected sealed override Type[] GetEventDefinedInvocationArgumentTypes()
        {
            return Array.Empty<Type>();
        }

        public TResult Invoke()
        {
            var call = PrepareInvoke();

            if (call is InvokableCallWithReturn<TResult> typed)
            {
                return typed.Invoke();
            }

            if (call is not null)
            {
                return (TResult)call.Invoke(Array.Empty<object>());
            }

            return default;
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, TResult> : UnityReturnDelegateBase
    {
        private static readonly Stack<object[]> _argsPool = new();

        protected sealed override Type ReturnType => typeof(TResult);

        internal override RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method)
        {
            return new InvokableCallWithReturn<T0, TResult>(target, method);
        }

        internal protected sealed override Type[] GetEventDefinedInvocationArgumentTypes()
        {
            return new Type[1] { typeof(T0) };
        }

        public TResult Invoke(T0 arg0)
        {
            var call = PrepareInvoke();

            if (call is InvokableCallWithReturn<T0, TResult> typed)
            {
                return typed.Invoke(arg0);
            }

            if (call is not null)
            {
                if (!_argsPool.TryPop(out object[] args)) args = new object[1];
                args[0] = arg0;

                var result = (TResult)call.Invoke(args);

                Array.Clear(args, 0, 1);
                _argsPool.Push(args);

                return result;
            }

            return default;
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, TResult> : UnityReturnDelegateBase
    {
        private static readonly Stack<object[]> _argsPool = new();

        protected sealed override Type ReturnType => typeof(TResult);

        internal override RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method)
        {
            return new InvokableCallWithReturn<T0, T1, TResult>(target, method);
        }

        internal protected sealed override Type[] GetEventDefinedInvocationArgumentTypes()
        {
            return new Type[2] { typeof(T0), typeof(T1) };
        }

        public TResult Invoke(T0 arg0, T1 arg1)
        {
            var call = PrepareInvoke();

            if (call is InvokableCallWithReturn<T0, T1, TResult> typed)
            {
                return typed.Invoke(arg0, arg1);
            }

            if (call is not null)
            {
                if (!_argsPool.TryPop(out object[] args)) args = new object[2];
                args[0] = arg0;
                args[1] = arg1;

                var result = (TResult)call.Invoke(args);

                Array.Clear(args, 0, 2);
                _argsPool.Push(args);

                return result;
            }

            return default;
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, T2, TResult> : UnityReturnDelegateBase
    {
        private static readonly Stack<object[]> _argsPool = new();

        protected sealed override Type ReturnType => typeof(TResult);

        internal override RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method)
        {
            return new InvokableCallWithReturn<T0, T1, T2, TResult>(target, method);
        }

        internal protected sealed override Type[] GetEventDefinedInvocationArgumentTypes()
        {
            return new Type[3] { typeof(T0), typeof(T1), typeof(T2) };
        }

        public TResult Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            var call = PrepareInvoke();

            if (call is InvokableCallWithReturn<T0, T1, T2, TResult> typed)
            {
                return typed.Invoke(arg0, arg1, arg2);
            }

            if (call is not null)
            {
                if (!_argsPool.TryPop(out object[] args)) args = new object[3];
                args[0] = arg0;
                args[1] = arg1;
                args[2] = arg2;

                var result = (TResult)call.Invoke(args);

                Array.Clear(args, 0, 3);
                _argsPool.Push(args);

                return result;
            }

            return default;
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, T2, T3, TResult> : UnityReturnDelegateBase
    {
        private static readonly Stack<object[]> _argsPool = new();

        protected sealed override Type ReturnType => typeof(TResult);

        internal override RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method)
        {
            return new InvokableCallWithReturn<T0, T1, T2, T3, TResult>(target, method);
        }

        internal protected sealed override Type[] GetEventDefinedInvocationArgumentTypes()
        {
            return new Type[4] { typeof(T0), typeof(T1), typeof(T2), typeof(T3) };
        }

        public TResult Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            var call = PrepareInvoke();

            if (call is InvokableCallWithReturn<T0, T1, T2, T3, TResult> typed)
            {
                return typed.Invoke(arg0, arg1, arg2, arg3);
            }

            if (call is not null)
            {
                if (!_argsPool.TryPop(out object[] args)) args = new object[4];
                args[0] = arg0;
                args[1] = arg1;
                args[2] = arg2;
                args[3] = arg3;

                var result = (TResult)call.Invoke(args);

                Array.Clear(args, 0, 4);
                _argsPool.Push(args);

                return result;
            }

            return default;
        }
    }
}
