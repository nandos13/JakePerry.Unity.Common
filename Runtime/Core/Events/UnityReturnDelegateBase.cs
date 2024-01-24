using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// Abstract base class for UnityReturnDelegates.
    /// </summary>
    [Serializable]
    public abstract class UnityReturnDelegateBase<TResult>
    {
        //UnityEngine.Events.UnityEvent;
        //UnityEngine.Events.UnityEvent<int>;

        [SerializeField]
        private UnityEngine.Object m_target;

        [SerializeField]
        private string m_targetAssemblyTypeName;

        [SerializeField]
        private string m_methodName;

        [SerializeField]
        private PersistentListenerMode m_mode = PersistentListenerMode.EventDefined;

        [SerializeField]
        private ArgumentCache m_arguments = new();

        private bool IsValid()
        {
            return !string.IsNullOrEmpty(m_targetAssemblyTypeName) && !string.IsNullOrEmpty(m_methodName);
        }

        protected abstract MethodInfo FindMethod_Impl(string name, Type targetObjType);

        protected static MethodInfo GetValidMethodInfo(Type objectType, string methodName, Type[] argTypes)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            while (objectType != typeof(object) && objectType != null)
            {
                var method = objectType.GetMethod(methodName, flags, null, argTypes, null);
                if (method is not null)
                {
                    //var parameters = method.GetParameters();
                    //foreach (var param in parameters)
                    //{
                    //
                    //}

                    return method;
                }

                objectType = objectType.BaseType;
                flags &= ~(BindingFlags.Public);
            }

            throw new NotImplementedException();
        }

        private MethodInfo FindMethod()
        {
            // TODO: Actually go over this and figure out why it's grabbing the type of the UnityEngine.Object argument...

            var argumentType = typeof(UnityEngine.Object);
            var objArgAssemblyTypeName = m_arguments.ObjectArgAssemblyTypeName;
            if (!string.IsNullOrEmpty(objArgAssemblyTypeName))
            {
                var type2 = Type.GetType(objArgAssemblyTypeName, throwOnError: false);
                if (type2 is not null) argumentType = type2;
            }

            var targetType = m_target != null
                ? m_target.GetType()
                : Type.GetType(m_targetAssemblyTypeName, throwOnError: false);

            return FindMethod(m_methodName, targetType, m_mode, argumentType);
        }

        /*
        internal MethodInfo FindMethod(PersistentCall call)
        {
            Type argumentType = typeof(Object);
            if (!string.IsNullOrEmpty(call.arguments.unityObjectArgumentAssemblyTypeName))
            {
                argumentType = Type.GetType(call.arguments.unityObjectArgumentAssemblyTypeName, throwOnError: false) ?? typeof(Object);
            }

            Type listenerType = ((call.target != null) ? call.target.GetType() : Type.GetType(call.targetAssemblyTypeName, throwOnError: false));
            return FindMethod(call.methodName, listenerType, call.mode, argumentType);
        }

        internal BaseInvokableCall GetRuntimeCall(UnityEventBase theEvent)
        {
            if (m_CallState == UnityEventCallState.RuntimeOnly && !Application.isPlaying)
            {
                return null;
            }

            if (m_CallState == UnityEventCallState.Off || theEvent == null)
            {
                return null;
            }

            MethodInfo methodInfo = theEvent.FindMethod(this);
            if ((object)methodInfo == null)
            {
                return null;
            }

            if (!methodInfo.IsStatic && target == null)
            {
                return null;
            }

            Object @object = (methodInfo.IsStatic ? null : target);
            return m_Mode switch
            {
                PersistentListenerMode.EventDefined => theEvent.GetDelegate(@object, methodInfo),
                PersistentListenerMode.Object => GetObjectCall(@object, methodInfo, m_Arguments),
                PersistentListenerMode.Float => new CachedInvokableCall<float>(@object, methodInfo, m_Arguments.floatArgument),
                PersistentListenerMode.Int => new CachedInvokableCall<int>(@object, methodInfo, m_Arguments.intArgument),
                PersistentListenerMode.String => new CachedInvokableCall<string>(@object, methodInfo, m_Arguments.stringArgument),
                PersistentListenerMode.Bool => new CachedInvokableCall<bool>(@object, methodInfo, m_Arguments.boolArgument),
                PersistentListenerMode.Void => new InvokableCall(@object, methodInfo),
                _ => null,
            };
        }
        */

        public string GetPersistentMethodName()
        {
            return m_methodName;
        }
    }

    // TODO: Move doc
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
