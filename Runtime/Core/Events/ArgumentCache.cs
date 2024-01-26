using System;
using UnityEngine;

namespace JakePerry.Unity.Events
{
    /// <summary>
    /// Implementation matching Unity's internal class of the same name.
    /// This object contains an invocation argument for the UnityReturnDelegate system.
    /// </summary>
    [Serializable]
    internal sealed class ArgumentCache : ISerializationCallbackReceiver
    {
        [SerializeField]
        private UnityEngine.Object m_objectArg;

        [SerializeField]
        private string m_objectArgAssemblyTypeName;

        [SerializeField]
        private int m_intArg;

        [SerializeField]
        private float m_floatArg;

        [SerializeField]
        private string m_stringArg;

        [SerializeField]
        private bool m_boolArg;

        public UnityEngine.Object ObjectArg => m_objectArg;

        public string ObjectArgAssemblyTypeName => m_objectArgAssemblyTypeName;

        public int IntArg => m_intArg;

        public float FloatArg => m_floatArg;

        public string StringArg => m_stringArg;

        public bool BoolArg => m_boolArg;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_objectArgAssemblyTypeName = UnityEventToolsWrapper.TidyAssemblyTypeName(m_objectArgAssemblyTypeName);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_objectArgAssemblyTypeName = UnityEventToolsWrapper.TidyAssemblyTypeName(m_objectArgAssemblyTypeName);
        }
    }
}
