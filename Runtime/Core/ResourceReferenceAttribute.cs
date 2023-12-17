using System;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Decorate a <see cref="SerializeGuid"/> field with this attribute to
    /// easily assign a resource to it.
    /// </summary>
    public sealed class ResourceReferenceAttribute : PropertyAttribute
    {
        private readonly Type m_resourceType;

        public Type ResourceType => m_resourceType ?? typeof(UnityEngine.Object);

        public ResourceReferenceAttribute() { }

        public ResourceReferenceAttribute(Type resourceType)
        {
            m_resourceType = resourceType;
        }
    }
}
