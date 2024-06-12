using System;
using UnityEngine;

namespace JakePerry.Unity
{
#pragma warning disable IDE1006 // Naming Styles

    /// <summary>
    /// A simple interface that indicates the implementing object is an <see cref="UnityEngine.Object"/>.
    /// Other interfaces can extend this interface if they are only intended to be implemented
    /// by an <see cref="UnityEngine.Object"/> class in order to provide convenient access
    /// to the object's name, hideFlags or instance ID without casing the object.
    /// </summary>
    /// <remarks>
    /// Note: Implementing this interface stipulates that the object is an <see cref="UnityEngine.Object"/>.
    /// Implementing on a non-<see cref="UnityEngine.Object"/> class may cause unexpected behaviour.
    /// </remarks>
    public interface IUnityObject
    {
        string name { get; set; }

        HideFlags hideFlags { get; set; }

        int GetInstanceID();
    }

#pragma warning restore IDE1006

    public static class IUnityObjectExtensions
    {
        /// <summary>
        /// Casts the current <see cref="IUnityObject"/> to an <see cref="UnityEngine.Object"/>.
        /// </summary>
        public static UnityEngine.Object ToUnityObject(this IUnityObject obj)
        {
            _ = obj ?? throw new ArgumentNullException(nameof(obj));

            if (obj is UnityEngine.Object o)
                return o;

            throw new InvalidOperationException($"The object is not an UnityEngine.Object. Object type: {obj.GetType()}.");
        }
    }
}
