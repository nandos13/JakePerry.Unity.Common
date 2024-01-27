using System;
using System.Reflection;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// Abstract base class for UnityReturnDelegates.
    /// </summary>
    [Serializable]
    public abstract class UnityReturnDelegateBase : ISerializationCallbackReceiver
    {
        // TODO: Remove these, just using them to easily peek Unity's code.
        //UnityEngine.Events.UnityEvent;
        //UnityEngine.Events.UnityEvent<int>;
        //UnityEngine.Events.UnityEvent<int, int, int, int>;

        [SerializeField]
        private UnityEngine.Object m_target;

        [SerializeField]
        private string m_targetAssemblyTypeName;

        [SerializeField]
        private string m_methodName;

        [SerializeField]
        private bool m_argumentsDefinedByEvent;

        [SerializeReference]
        private InvocationArgument[] m_arguments;

        private bool m_dirty = true;
        private RuntimeInvocableCall m_call;

        protected Type TargetType
        {
            get
            {
                return m_target != null
                    ? m_target.GetType()
                    : Type.GetType(m_targetAssemblyTypeName, throwOnError: false);
            }
        }

        protected abstract Type[] GetEventDefinedInvocationArgumentTypes();
        internal abstract RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method);

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

                    // TODO: WE NEED TO ACTUALLY VALIDATE THE RETURN TYPE HERE! THIS ISNT DONE BY UNITY BECAUSE THEY ONLY USE VOID METHODS
                    return method;
                }

            AFTER_CHECK_METHOD:

                objectType = objectType.BaseType;
                flags &= ~(BindingFlags.Public);
            }

            return null;
        }

        private static Type[] GetCachedInvocationArgumentTypes(InvocationArgument[] args)
        {
            var argCount = args?.Length ?? 0;
            if (argCount == 0)
            {
                return Array.Empty<Type>();
            }

            var result = new Type[argCount];
            for (int i = 0; i < argCount; ++i)
            {
                result[i] = args[i].ArgumentType;
            }

            return result;
        }

        private static object[] GetCachedInvocationArgumentValues(InvocationArgument[] args)
        {
            var argCount = args?.Length ?? 0;
            if (argCount == 0)
            {
                return Array.Empty<object>();
            }

            var result = new object[argCount];
            for (int i = 0; i < argCount; ++i)
            {
                result[i] = args[i].ArgumentValue;
            }

            return result;
        }

        private void DirtyRuntimeCall()
        {
            m_call = null;
            m_dirty = true;
        }

        private MethodInfo FindMethod(Type targetType)
        {
            var argTypes = m_argumentsDefinedByEvent
                ? GetEventDefinedInvocationArgumentTypes()
                : GetCachedInvocationArgumentTypes(m_arguments);

            return GetValidMethodInfo(targetType, m_methodName, argTypes);
        }

        internal RuntimeInvocableCall PrepareInvoke()
        {
            if (m_dirty)
            {
                var type = TargetType;
                if (type != null)
                {
                    var method = FindMethod(type);
                    if (method != null)
                    {
                        if (method.IsStatic == (m_target == null))
                        {
                            var target = method.IsStatic ? null : m_target;

                            if (m_argumentsDefinedByEvent)
                            {
                                // TODO: Need to pass the invocation target and method to child impl.
                                m_call = ConstructDelegateCall(target, method);
                            }
                            else
                            {
                                var cachedArguments = GetCachedInvocationArgumentValues(m_arguments);
                                m_call = new CachedInvocableCall(target, method, cachedArguments);
                            }
                        }
                    }
                }

                m_dirty = false;
            }

            return m_call;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => DirtyRuntimeCall();

        void ISerializationCallbackReceiver.OnAfterDeserialize() => DirtyRuntimeCall();
    }
}
