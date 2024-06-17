using System;
using UnityEngine;

namespace JakePerry.Unity
{
    // TODO: Consider how this might be improved without the use of an interface.
    // I had the idea that we could serialize an object and a propertyPath string,
    // similar to how Unity's SerializedProperty works.
    // The code I already use to resolve the target member via reflection could in theory
    // also be used at runtime, as long as the editor-specific constructors are compile defined.
    // Further support could also be added for dictionaries, or really anything with an indexer
    // property with string or int key.
    // For now, this is too out of scope, so I leave this comment for another time.

    /// <summary>
    /// A serializable data container holding a value of type <typeparamref name="T"/>.
    /// This value may be serialized in place, or obtained via a referenced object.
    /// </summary>
    /// <typeparam name="T">
    /// Type of value held by the object.
    /// </typeparam>
    [Serializable]
    public struct LinkableData<T> : ISerializationCallbackReceiver
    {
        [SerializeField]
        private bool m_linked;

        [SerializeField]
        private T m_value;

        [SerializeField]
        private InspectableInterface<IValueFromUnityObject<T>> m_source;

        /// <summary>
        /// Obtain the value stored by this data container.
        /// </summary>
        public T Value
        {
            get
            {
                if (m_linked)
                {
                    var src = m_source.GetReferencedInterface();
                    if (src is not null)
                    {
                        return src.Value;
                    }

                    return default;
                }

                return m_value;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() { /* Do nothing */ }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (!m_linked) m_source = default;
        }

        public static implicit operator LinkableData<T>(T value)
        {
            return new LinkableData<T>() { m_value = value };
        }

        public static implicit operator T(LinkableData<T> dat)
        {
            return dat.Value;
        }
    }

    /// <summary>
    /// Describes an object inheriting <see cref="UnityEngine.Object"/> which can
    /// provide a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// Type of value held by the object.
    /// </typeparam>
    public interface IValueFromUnityObject<T> : IUnityObject
    {
        // TODO: Move this type to a new document
        T Value { get; }
    }
}
