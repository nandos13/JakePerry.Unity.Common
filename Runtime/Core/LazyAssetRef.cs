using System;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Weak-typed lazy asset reference for a loadable Resources asset.
    /// </summary>
    /// <typeparam name="T">
    /// Asset type.
    /// </typeparam>
    [Serializable]
    public struct LazyAssetRef<T> : IEquatable<LazyAssetRef<T>>, IFormattable
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif // UNITY_EDITOR
        where T : UnityEngine.Object
    {
        [SerializeField]
        private SerializeGuid m_guid;

        public SerializeGuid Guid => m_guid;

        public LazyAssetRef(SerializeGuid guid)
        {
            m_guid = guid;
        }

        public LazyAssetRef(Guid guid) : this(new SerializeGuid(guid)) { }

        public LazyAssetRef(string guid) : this(new SerializeGuid(guid)) { }

        public T Load()
        {
            if (m_guid.IsDefault) return null;

            var inst = LazyAssetRefManifest.GetInstance();
            if (inst != null &&
                inst.TryGetResourcePath(m_guid.UnityGuidString, out string resourcePath))
            {
                return Resources.Load<T>(resourcePath);
            }

            return null;
        }

        public bool TryLoad(out T resource)
        {
            resource = Load() as T;
            return resource != null;
        }

        public bool Equals(LazyAssetRef<T> other)
        {
            return m_guid.Equals(other.m_guid);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return m_guid.IsDefault;
            if (obj is LazyAssetRef<T> other) return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            return m_guid.GetHashCode();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            string type = $"LazyAssetRef<{typeof(T).Name}>";

            return m_guid.IsDefault
                ? $"Invalid {type}"
                : $"{type} {m_guid.ToString(format, formatProvider)}";
        }

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public override string ToString()
        {
            return ToString("N", null);
        }

#if UNITY_EDITOR

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            //Debug.LogError("Foo");
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Do nothing
        }

#endif // UNITY_EDITOR

        public static bool operator ==(LazyAssetRef<T> a, LazyAssetRef<T> b) => a.Equals(b);
        public static bool operator !=(LazyAssetRef<T> a, LazyAssetRef<T> b) => !(a == b);

        public static bool operator ==(LazyAssetRef<T> a, LazyAssetRef<T>? b) => b.HasValue && a.Equals(b.Value);
        public static bool operator !=(LazyAssetRef<T> a, LazyAssetRef<T>? b) => !(a == b);

        public static bool operator ==(LazyAssetRef<T>? a, LazyAssetRef<T> b) => a.HasValue && a.Value.Equals(b);
        public static bool operator !=(LazyAssetRef<T>? a, LazyAssetRef<T> b) => !(a == b);
    }
}
