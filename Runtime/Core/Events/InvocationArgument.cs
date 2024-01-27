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
    internal sealed class IntArgument : InvocationArgument
    {
        [SerializeField]
        private int m_value;

        internal override Type ArgumentType => typeof(int);
        internal override object ArgumentValue => (object)m_value;
    }

    [Serializable]
    internal sealed class FloatArgument : InvocationArgument
    {
        [SerializeField]
        private float m_value;

        internal override Type ArgumentType => typeof(float);
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
    internal sealed class BoolArgument : InvocationArgument
    {
        [SerializeField]
        private bool m_value;

        internal override Type ArgumentType => typeof(bool);
        internal override object ArgumentValue => (object)m_value;
    }

    [Serializable]
    internal sealed class ObjectArgument : InvocationArgument, ISerializationCallbackReceiver
    {
        [SerializeField]
        private UnityEngine.Object m_value;

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

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_assemblyTypeName = UnityEventToolsWrapper.TidyAssemblyTypeName(m_assemblyTypeName);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_assemblyTypeName = UnityEventToolsWrapper.TidyAssemblyTypeName(m_assemblyTypeName);
        }
    }
}
