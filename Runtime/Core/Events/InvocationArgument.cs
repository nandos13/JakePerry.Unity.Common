using System;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// The base class for all serializable invocation arguments that are set up via the inspector.
    /// This is a nicer approach to the implementation of Unity's internal ArgumentCache class.
    /// </summary>
    [Serializable]
    internal abstract class InvocationArgument
    {
        internal abstract Type ArgumentType { get; }
        internal abstract object ArgumentValue { get; }

        internal virtual string Debug_GetSerializedTypeName() => string.Empty;
    }

    [Serializable]
    internal abstract class StructArgument<T> : InvocationArgument
        where T : struct
    {
        internal sealed override Type ArgumentType => typeof(T);
    }

    [Serializable]
    internal abstract class ParameterTypedArgument : InvocationArgument
    {
        // TODO: This needs to be validated via the 'tidy' method when the value is set in inspector.
        [SerializeField]
        private string m_parameterTypeName;

        private Type m_resolvedType;

        internal sealed override Type ArgumentType
        {
            get
            {
                if (m_resolvedType is null)
                {
                    var n = m_parameterTypeName;
                    if (!string.IsNullOrEmpty(n))
                    {
                        var type2 = Type.GetType(n, throwOnError: false);
                        if (type2 is not null)
                        {
                            m_resolvedType = type2;
                            return type2;
                        }
                    }

                    m_resolvedType = typeof(UnityEngine.Object);
                }

                return m_resolvedType;
            }
        }

        internal sealed override string Debug_GetSerializedTypeName()
        {
            return m_parameterTypeName ?? string.Empty;
        }
    }

    [Serializable]
    internal sealed class IntArgument : StructArgument<int>
    {
        [SerializeField]
        private int m_value;

        internal override object ArgumentValue => (object)m_value;
    }

    [Serializable]
    internal sealed class FloatArgument : StructArgument<float>
    {
        [SerializeField]
        private float m_value;

        internal override object ArgumentValue => (object)m_value;
    }

    [Serializable]
    internal sealed class BoolArgument : StructArgument<bool>
    {
        [SerializeField]
        private bool m_value;

        internal override object ArgumentValue => (object)m_value;
    }

    [Serializable]
    internal sealed class StringArgument : InvocationArgument
    {
        [SerializeField]
        private string m_value;

        internal override Type ArgumentType => typeof(string);
        internal override object ArgumentValue => (object)m_value;
    }

    [Serializable]
    internal sealed class ObjectArgument : ParameterTypedArgument
    {
        [SerializeField]
        private UnityEngine.Object m_value;

        internal override object ArgumentValue => (object)m_value;
    }

    [Serializable]
    internal sealed class ExtendedInvocationArgument : ParameterTypedArgument
    {
        [SerializeReference]
        private SerializableMethodArgument m_arg;

        internal override object ArgumentValue
        {
            get
            {
                var arg = m_arg;
                if (arg is not null)
                {
                    var field = SerializableMethodArgument.GetArgumentValueField(arg.GetType());
                    if (field is not null)
                    {
                        return field.GetValue(arg);
                    }
                }

                return null;
            }
        }
    }
}
