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
        [SerializeField]
        private UnityEngine.Object m_target;

        [SerializeField]
        private SerializeTypeDefinition m_staticTargetType;

        [SerializeField]
        private bool m_targetingStaticMember;

        [SerializeField]
        private string m_methodName;

        [SerializeField]
        private bool m_argumentsDefinedByEvent;

        [SerializeReference]
        private InvocationArgument[] m_arguments;

        [SerializeField]
        private byte m_policy;

#if UNITY_EDITOR

        [SerializeField]
        private byte m_editorBehaviour;

#endif // UNITY_EDITOR

        private bool m_dirty = true;
        private RuntimeInvocableCall m_call;

        protected abstract Type ReturnType { get; }

        /// <summary>
        /// Indicates the error handling policy that should be enacted when the invocation
        /// target is a destroyed <see cref="UnityEngine.Object"/>.
        /// </summary>
        public TargetDestroyedErrorHandlingPolicy Policy
        {
            get => (TargetDestroyedErrorHandlingPolicy)m_policy;
            set
            {
                if (value < TargetDestroyedErrorHandlingPolicy.Default ||
                    value > TargetDestroyedErrorHandlingPolicy.ThrowException)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                m_policy = (byte)(int)value;
            }
        }

        internal protected abstract Type[] GetEventDefinedInvocationArgumentTypes();
        internal abstract RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method);

        private static MethodInfo GetValidMethodInfo(Type objectType, string methodName, Type returnType, Type[] argTypes)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            MethodInfo matchWithIncorrectReturn = null;

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
                    // According to this link: https://stackoverflow.com/a/61888753
                    // This refers "to a Type object whose IsGenericParameter property is true",
                    // so it would only apply if objectType was a generic type definition (ie. typeof(List<>)),
                    // which is clearly an invalid use case.
                    // 
                    // I need to investigate further and see if there are more unexpected circumstances
                    // in which this check actually does anything useful. Otherwise it's entirely possible
                    // it came from an early iteration of Unity's UnityEvent code and it was never removed
                    // because other programmers were just as confused as I am...
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

                    /* Note:
                     * There is no way to pass the expected return type to the Type.GetMethod call. We also want to support
                     * methods which have a more derived return type.
                     * If the return type is incompatible, simply pretend we didn't see it. This may occur for one of
                     * the following reasons:
                     * - The target method's signature changed since this delegate was serialized (in which case,
                     *   ignoring it is the correct behaviour).
                     * or
                     * - The target method is declared by a base type and will be found in a subsequent loop iteration.
                     *   The current method is either hiding the base method (via the 'new' keyword), or both the current
                     *   and target method are private.
                     */
                    if (returnType.IsAssignableFrom(method.ReturnType))
                    {
                        matchWithIncorrectReturn = method;
                        goto AFTER_CHECK_METHOD;
                    }

                    return method;
                }

            AFTER_CHECK_METHOD:

                objectType = objectType.BaseType;
                flags &= ~(BindingFlags.Public);
            }

            if (matchWithIncorrectReturn is not null)
            {
                if (ReturnDelegatesConfig.ErrorLoggingEnabled)
                {
                    // TODO: Log an error stating that a method was found but return type was incorrect.
                    // Likely a signature change since serialization.
                    ReturnDelegatesUtility.LogError("");
                }
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

        /// <summary>
        /// Checks if invocation is allowed &amp; handles logging an error or throwing an exception
        /// in accordance with the current error handling policy when it is not allowed..
        /// </summary>
        private static bool VerifyInvokeIsAllowed(RuntimeInvocableCall call, TargetDestroyedErrorHandlingPolicy policy)
        {
            bool allowed = call.AllowInvoke;
            if (!allowed)
            {
                if (policy == TargetDestroyedErrorHandlingPolicy.Default)
                {
                    policy = ReturnDelegatesConfig.TargetDestroyedPolicy;
                }

                // TODO: Augment error & exception with some useful data. Call site, target object id, method name perhaps...
                if (policy == TargetDestroyedErrorHandlingPolicy.LogError)
                {
                    ReturnDelegatesUtility.LogError("Target object is destroyed! Invocation will not proceed & a default value will be returned.");
                }
                else if (policy == TargetDestroyedErrorHandlingPolicy.ThrowException)
                {
                    throw new InvocationTargetDestroyedException();
                }
            }

            return allowed;
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

            var returnType = ReturnType;
            return GetValidMethodInfo(targetType, m_methodName, returnType, argTypes);
        }

        private Type ResolveInvocationType()
        {
            if (!m_targetingStaticMember)
            {
                if (m_target is not null)
                {
                    return m_target.GetType();
                }
                else
                {
                    // TODO: Report an error here. Change policy to more generic instead of specifically for destroyed targets?
                }
            }
            else if (!m_staticTargetType.IsNull)
            {
                //var type = Type.GetType(m_staticTypeTarget, throwOnError: false);
                //if (type is not null) return type;

                // TODO: Error that static type wasnt found/resolved.
            }
            else
            {
                // TODO: Error that nothing is set 
            }

            return null;
        }

        private void ResolveRuntimeCallIfDirty()
        {
            if (m_dirty)
            {
                var type = ResolveInvocationType();
                if (type != null)
                {
                    var method = FindMethod(type);
                    if (method != null)
                    {
                        if (method.IsStatic == (m_target is null))
                        {
                            var target = method.IsStatic ? null : m_target;

                            if (m_argumentsDefinedByEvent)
                            {
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
        }

        /// <summary>
        /// Performs the necessary reflection logic to resolve the runtime invocable call &amp;
        /// checks that the call is valid.
        /// </summary>
        /// <returns>
        /// The <see cref="RuntimeInvocableCall"/> instance that should be invoked by the derived class,
        /// if one is available and valid; Otherwise, returns <see langword="null"/>.
        /// </returns>
        internal RuntimeInvocableCall PrepareInvoke()
        {
            ResolveRuntimeCallIfDirty();

            var call = m_call;

            if (call is not null)
            {
                var policy = this.Policy;
                if (!VerifyInvokeIsAllowed(call, policy))
                {
                    call = null;
                }
            }

            return call;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => DirtyRuntimeCall();

        void ISerializationCallbackReceiver.OnAfterDeserialize() => DirtyRuntimeCall();
    }
}
