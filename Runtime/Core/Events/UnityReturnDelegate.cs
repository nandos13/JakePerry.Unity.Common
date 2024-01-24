using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    [Serializable]
    public class UnityReturnDelegate<TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
        {
            return UnityReturnDelegateBase<TResult>.GetValidMethodInfo(targetObjType, name, Array.Empty<Type>());
        }

        public TResult Invoke()
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
        {
            var argTypes = new Type[1] { typeof(T0) };
            return UnityReturnDelegateBase<TResult>.GetValidMethodInfo(targetObjType, name, argTypes);
        }

        public TResult Invoke(T0 arg0)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
        {
            var argTypes = new Type[2] { typeof(T0), typeof(T1) };
            return UnityReturnDelegateBase<TResult>.GetValidMethodInfo(targetObjType, name, argTypes);
        }

        public TResult Invoke(T0 arg0, T1 arg1)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, T2, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
        {
            var argTypes = new Type[3] { typeof(T0), typeof(T1), typeof(T2) };
            return UnityReturnDelegateBase<TResult>.GetValidMethodInfo(targetObjType, name, argTypes);
        }

        public TResult Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, T2, T3, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
        {
            var argTypes = new Type[4] { typeof(T0), typeof(T1), typeof(T2), typeof(T3) };
            return UnityReturnDelegateBase<TResult>.GetValidMethodInfo(targetObjType, name, argTypes);
        }

        public TResult Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            throw new NotImplementedException();
        }
    }
}
