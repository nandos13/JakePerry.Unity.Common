using System;
using UnityEngine;

namespace JakePerry.Unity
{
#pragma warning disable IDE1006 // Naming Styles

    /// <summary>
    /// A simple interface that indicates the implementing object is a <see cref="Component"/>.
    /// Other interfaces can extend this interface if they are only intended to be implemented
    /// by a <see cref="Component"/> class in order to provide convenient access
    /// to the <see cref="GameObject"/> or <see cref="Transform"/> without casing the object.
    /// </summary>
    /// <remarks>
    /// Note: Implementing this interface stipulates that the object is a <see cref="Component"/>.
    /// Implementing on a non-<see cref="Component"/> class may cause unexpected behaviour.
    /// </remarks>
    public interface IComponent
    {
        public GameObject gameObject { get; }

        public Transform transform { get; }
    }

#pragma warning restore IDE1006

    public static class IComponentExtensions
    {
        /// <summary>
        /// Casts the current <see cref="IComponent"/> to a <see cref="Component"/>.
        /// </summary>
        public static Component ToComponent(this IComponent component)
        {
            _ = component ?? throw new ArgumentNullException(nameof(component));

            if (component is Component c)
                return c;

            throw new InvalidOperationException($"The object is not a Component. Object type: {component.GetType()}.");
        }
    }
}
