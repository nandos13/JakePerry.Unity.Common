using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// Abstract base class for UnityReturnDelegates.
    /// </summary>
    [Serializable]
    public abstract class UnityReturnDelegateBase : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR

        internal static class EditorBehaviours
        {
            internal const byte kReturnDefaultValue = 0;
            internal const byte kReturnMockValue = 1;
            internal const byte kInvokeInEditMode = 2;
        }

#endif // UNITY_EDITOR

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
        private byte m_invocationFailedPolicy;

        private bool m_dirty = true;
        private RuntimeInvocableCall m_call;

        internal protected abstract Type ReturnType { get; }

        /// <summary>
        /// Indicates the error handling policy that should be enacted
        /// when invocation fails due to an exception.
        /// </summary>
        public ErrorHandlingPolicy InvocationFailedPolicy
        {
            get => (ErrorHandlingPolicy)m_invocationFailedPolicy;
            set => m_invocationFailedPolicy = value.ToByte();
        }

#if UNITY_EDITOR
        internal abstract IInvocableCall ConstructEditorModeCall();
#endif

        internal protected abstract Type[] GetEventDefinedInvocationArgumentTypes();
        internal abstract RuntimeInvocableCall ConstructDelegateCall(object target, MethodInfo method);

        internal static MethodInfo GetValidMethodInfo(Type objectType, bool @static, string methodName, Type returnType, Type[] argTypes, out string error)
        {
            const BindingFlags kFlagsStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags kFlagsInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            error = null;

            var flags = @static ? kFlagsStatic : kFlagsInstance;

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
                        if (argTypes[num++].IsPrimitive != param.ParameterType.IsPrimitive)
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
                    if (!returnType.IsAssignableFrom(method.ReturnType))
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
                error = "Method was found but has an unexpected return type. This may indicate that the method has been refactored " +
                    "since this delegate was serialized, or the delegate referenced a method on a parent type which has since been removed.";
            }

            return null;
        }

        internal static MethodInfo GetValidMethodInfo(Type objectType, bool @static, string methodName, Type returnType, Type[] argTypes)
        {
            var method = GetValidMethodInfo(objectType, @static, methodName, returnType, argTypes, out string error);

            if (error is not null && ReturnDelegatesConfig.ErrorLoggingEnabled)
            {
                ReturnDelegatesUtility.LogError(error);
            }

            return method;
        }

        protected void HandleInvocationException(Exception exception)
        {
            if (exception is null) return;

            var policy = InvocationFailedPolicy;
            if (policy == ErrorHandlingPolicy.Default)
            {
                policy = ReturnDelegatesConfig.InvocationFailedPolicy;
            }

            if (policy == ErrorHandlingPolicy.LogError)
            {
                // Special case: better logs for destroyed invocation target
                if (exception is InvocationTargetDestroyedException)
                {
                    ReturnDelegatesUtility.LogError("Target object is destroyed! " +
                        "Invocation will not proceed and a default value will be returned.");
                }
                else
                {
                    ReturnDelegatesUtility.LogError($"Exception of type {exception.GetType()} occurred during invocation " +
                        "(see the following exception log for details). " +
                        "Invocation will not proceed and a default value will be returned.");

                    Debug.LogException(exception);
                }
            }
            else if (policy == ErrorHandlingPolicy.ThrowException)
            {
                if (exception is InvokeFailedException)
                {
                    ExceptionDispatchInfo.Throw(exception);
                }

                throw new InvokeFailedException(exception);
            }
        }

        private void DirtyRuntimeCall()
        {
            m_call = null;
            m_dirty = true;
        }

        private MethodInfo FindMethod(Type targetType)
        {
            Type[] argTypes;
            if (m_argumentsDefinedByEvent)
            {
                argTypes = GetEventDefinedInvocationArgumentTypes();
            }
            else
            {
                var policy = InvocationFailedPolicy;
                if (policy == ErrorHandlingPolicy.Default)
                {
                    policy = ReturnDelegatesConfig.InvocationFailedPolicy;
                }

                var args = m_arguments;
                var argCount = args?.Length ?? 0;
                if (argCount == 0)
                {
                    argTypes = Array.Empty<Type>();
                }
                else
                {
                    var result = new Type[argCount];
                    for (int i = 0; i < argCount; ++i)
                    {
                        var t = args[i].ArgumentType;

                        if (t is null)
                        {
                            if (policy > ErrorHandlingPolicy.Ignore)
                            {
                                var err =
                                    "Cannot resolve method; one or more serialized arguments returned a null type. The data must be manually fixed and reserialized. " +
                                    "See below for more info:\n" +
                                    "This error can occur if the argument's type is renamed, relocated or removed from the project after the data was saved.\n" +
                                    "Argument index: " +
                                    i.ToString() +
                                    "\nSerialized type name: " +
                                    args[i].Debug_GetSerializedTypeName();

                                if (policy == ErrorHandlingPolicy.LogError)
                                {
                                    ReturnDelegatesUtility.LogError(err);
                                }
                                else
                                {
                                    throw new InvocationMethodNotFoundException(err);
                                }
                            }

                            // There is a problem with serialized argument data,
                            // in which case the correct method will not be resolvable.
                            return null;
                        }

                        result[i] = t;
                    }

                    argTypes = result;
                }
            }

            // TODO: GetValidMethodInfo might log an error, without consulting the current
            // policy. Refactor so it doesn't have the 'out string error'. Maybe if a match is
            // found with wrong return type, throw and the inspector class can catch it.
            var returnType = ReturnType;
            return GetValidMethodInfo(targetType, m_targetingStaticMember, m_methodName, returnType, argTypes);
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
                        // TODO: Check if this condition is correct. Is this instead meant to be checking m_targetingStaticMember?
                        // This code may be correct if there's a possibility FindMethod might return a method that is the wrong
                        // instance/static
                        if (method.IsStatic == (m_target is null))
                        {
                            var target = method.IsStatic ? null : m_target;

                            if (m_argumentsDefinedByEvent)
                            {
                                m_call = ConstructDelegateCall(target, method);
                            }
                            else
                            {
                                var cachedArguments = InvocationArgument.GetArgumentValues(m_arguments);
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
        /// The <see cref="IInvocableCall"/> instance that should be invoked by the derived class,
        /// if one is available and valid; Otherwise, returns <see langword="null"/>.
        /// </returns>
        internal IInvocableCall PrepareInvoke()
        {
            IInvocableCall call;

#if UNITY_EDITOR

            if (!UnityEditor.EditorApplication.isPlaying)
            {
                call = ConstructEditorModeCall();
                if (call is not null)
                {
                    return call;
                }
            }

#endif // UNITY_EDITOR

            ResolveRuntimeCallIfDirty();

            return m_call;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => DirtyRuntimeCall();

        void ISerializationCallbackReceiver.OnAfterDeserialize() => DirtyRuntimeCall();
    }

    [Serializable]
    public abstract class UnityReturnDelegateBase<TResult> : UnityReturnDelegateBase
    {
#if UNITY_EDITOR

        [SerializeField]
        private byte m_editorBehaviour;

        /* TODO: Revise using [SerializeReference] here and for other serialized arguments.
         * Unfortunately, I came across several issues.
         * Because the field is a generic type, any types that use a struct type (value type
         * that isn't one of Unity's native handled types like float) for TResult
         * cause Unity to log an error on compilation.
         * I had an idea that will need more investigation, leaving notes here for another time:
         * 1. Change this field to an object type instead of generic TResult.
         * 2. Create a wrapper type that holds the actual argument.
         * 3. Again try having two separate fields, one SerializeField, one SerializeReference.
         * 4. Try using SerializedPropertyType to detect for managed ref, only use the SR one in that case.
         */
        [SerializeField]
        //[SerializeReference]
        private TResult m_editorMockValue;

#endif // UNITY_EDITOR

        internal protected sealed override Type ReturnType => typeof(TResult);

#if UNITY_EDITOR
        internal sealed override IInvocableCall ConstructEditorModeCall()
        {
            if (m_editorBehaviour == EditorBehaviours.kReturnDefaultValue)
            {
                return new MockInvocableCall<TResult>(default);
            }
            else if (m_editorBehaviour == EditorBehaviours.kReturnMockValue)
            {
                return new MockInvocableCall<TResult>(m_editorMockValue);
            }

            return null;
        }
#endif // UNITY_EDITOR
    }
}
