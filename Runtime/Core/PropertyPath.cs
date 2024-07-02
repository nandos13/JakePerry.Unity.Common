using System;
using UnityEngine;

namespace JakePerry.Unity
{
    [Serializable]
    public struct PropertyPath
    {
        [SerializeField]
        private UnityEngine.Object m_rootObject;

        [SerializeField]
        private string m_propertyPath;

        [SerializeField]
        private ErrorHandlingPolicy m_errorPolicy;
    }
}
