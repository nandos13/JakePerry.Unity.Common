using System;
using UnityEngine;

namespace JakePerry.Unity
{
    [Serializable]
    public struct InspectableInterface<T>
        where T : class
    {
        [SerializeField]
        private UnityEngine.Object m_targetObject;

        public T GetReferencedInterface()
        {
            return m_targetObject is T cast ? cast : null;
        }

        public static implicit operator T(InspectableInterface<T> source)
        {
            return source.GetReferencedInterface();
        }
    }
}
