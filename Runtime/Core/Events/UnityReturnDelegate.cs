using System;
using System.Reflection;

namespace JakePerry.Unity.Events
{
    [Serializable]
    public class UnityReturnDelegate<TResult> : UnityReturnDelegateBase<TResult>
    {
        private bool m_dirty = true;
        private InvokableCallWithReturn<TResult> m_func;

        protected override void DirtyRuntimeCall()
        {
            m_dirty = true;
            m_func = null;
        }

        protected override Type[] GetEventDefinedInvocationArgTypes()
        {
            return Array.Empty<Type>();
        }

        public TResult Invoke()
        {
            if (m_dirty)
            {
                if (IsValid())
                {
                    // TODO
                }
                m_dirty = false;
            }

            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override Type[] GetEventDefinedInvocationArgTypes()
        {
            return new Type[1] { typeof(T0) };
        }

        public TResult Invoke(T0 arg0)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override Type[] GetEventDefinedInvocationArgTypes()
        {
            return new Type[2] { typeof(T0), typeof(T1) };
        }

        public TResult Invoke(T0 arg0, T1 arg1)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, T2, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override Type[] GetEventDefinedInvocationArgTypes()
        {
            return new Type[3] { typeof(T0), typeof(T1), typeof(T2) };
        }

        public TResult Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class UnityReturnDelegate<T0, T1, T2, T3, TResult> : UnityReturnDelegateBase<TResult>
    {
        protected override Type[] GetEventDefinedInvocationArgTypes()
        {
            return new Type[4] { typeof(T0), typeof(T1), typeof(T2), typeof(T3) };
        }

        public TResult Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            throw new NotImplementedException();
        }
    }
}
