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
    public abstract class UnityReturnDelegateBase<TResult> : ISerializationCallbackReceiver
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

        private static MethodInfo GetValidMethodInfo(Type objectType, string methodName, Type[] argTypes)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            while (objectType != typeof(object) && objectType != null)
            {
                var method = objectType.GetMethod(methodName, flags, null, argTypes, null);
                if (method is not null)
                {
                    // TODO: Investigate this code...
                    // We know that the parameter types should match because they're passed into the
                    // GetMethod call above. For some reason Unity checks the IsPrimitive property
                    // and potentially skips the method if they don't match. Perhaps this has something
                    // to do with method hiding, or private methods with the same signature declared
                    // on a base + child chass?
                    // 
                    // IsPrimitive documentation states...
                    // If the current Type represents a generic type, or a type parameter in the definition
                    // of a generic type or generic method, this property always returns false.
                    // 
                    // I was unable to replicate this in a quick Linqpad test, so I may be
                    // interpreting it wrong. More testing required...
                    var parameters = method.GetParameters();
                    int num = 0;
                    foreach (var param in parameters)
                    {
                        if (argTypes[num].IsPrimitive != param.ParameterType.IsPrimitive)
                        {
                            goto AFTER_CHECK_METHOD;
                        }
                    }
                    // ---

                    return method;
                }

                AFTER_CHECK_METHOD:

                objectType = objectType.BaseType;
                flags &= ~(BindingFlags.Public);
            }

            return null;
        }

        protected abstract void DirtyRuntimeCall();

        protected abstract Type[] GetEventDefinedInvocationArgTypes();

        private Type[] GetInvocationArgTypes()
        {
            var mode = m_mode;

            if (mode == PersistentListenerMode.Object)
            {
                var objArgAssemblyTypeName = m_arguments.ObjectArgAssemblyTypeName;
                if (!string.IsNullOrEmpty(objArgAssemblyTypeName))
                {
                    var type2 = Type.GetType(objArgAssemblyTypeName, throwOnError: false);
                    if (type2 is not null)
                    {
                        return new Type[1] { type2 };
                    }
                }

                return new Type[1] { typeof(UnityEngine.Object) };
            }

            return mode switch
            {
                PersistentListenerMode.EventDefined => GetEventDefinedInvocationArgTypes(),
                PersistentListenerMode.Void => Array.Empty<Type>(),
                // TODO: Cache these
                PersistentListenerMode.Float => new Type[1] { typeof(float) },
                PersistentListenerMode.Int => new Type[1] { typeof(int) },
                PersistentListenerMode.Bool => new Type[1] { typeof(bool) },
                PersistentListenerMode.String => new Type[1] { typeof(string) },
                _ => null,
            };
        }

        private MethodInfo FindMethod(Type targetType)
        {
            var argTypes = GetInvocationArgTypes();
            return GetValidMethodInfo(targetType, m_methodName, argTypes);
        }

        private MethodInfo FindMethod()
        {
            var targetType = m_target != null
                ? m_target.GetType()
                : Type.GetType(m_targetAssemblyTypeName, throwOnError: false);

            return FindMethod(targetType);
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

        void ISerializationCallbackReceiver.OnBeforeSerialize() => DirtyRuntimeCall();

        void ISerializationCallbackReceiver.OnAfterDeserialize() => DirtyRuntimeCall();
    }
}
