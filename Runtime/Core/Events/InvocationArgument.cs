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
    }

    [Serializable]
    internal abstract class StructArgument<T> : InvocationArgument
        where T : struct
    {
        internal sealed override Type ArgumentType => typeof(T);
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
    internal sealed class StringArgument : InvocationArgument
    {
        [SerializeField]
        private string m_value;

        internal override Type ArgumentType => typeof(string);
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
    internal sealed class ObjectArgument : InvocationArgument
    {
        [SerializeField]
        private UnityEngine.Object m_value;

        // TODO: This needs to be validated via the 'tidy' method when the value is set in inspector.
        [SerializeField]
        private string m_assemblyTypeName;

        private Type m_resolvedType;

        internal override Type ArgumentType
        {
            get
            {
                if (m_resolvedType is null)
                {
                    var assemblyTypeName = m_assemblyTypeName;
                    if (!string.IsNullOrEmpty(assemblyTypeName))
                    {
                        var type2 = Type.GetType(assemblyTypeName, throwOnError: false);
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

        internal override object ArgumentValue => (object)m_value;
    }
}
