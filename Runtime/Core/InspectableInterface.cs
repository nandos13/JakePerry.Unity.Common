using System;
using UnityEngine;

using static JakePerry.Unity.InspectableInterface;

namespace JakePerry.Unity
{
    [Serializable]
    public struct InspectableInterface<T>
        where T : class
    {
        [SerializeField]
        private UnityEngine.Object m_targetObject;

        internal UnityEngine.Object TargetObject => m_targetObject;

        public T GetReferencedInterface()
        {
            return CastUnityObjectToInterface<T>(m_targetObject);
        }

        public static implicit operator T(InspectableInterface<T> source)
        {
            return source.GetReferencedInterface();
        }
    }
}
