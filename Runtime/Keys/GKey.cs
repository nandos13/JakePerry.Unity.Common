using System;
using UnityEngine;

namespace JakePerry.Unity.GuidKeys
{
    // TODO: Documentation
    [Serializable]
    public struct GKey : IEquatable<GKey>
    {
        [SerializeField]
        private SerializeGuid m_guid;

        /// <summary>
        /// The <see cref="Guid"/> backing this key.
        /// </summary>
        public Guid Guid => (Guid)m_guid;

        public bool Equals(GKey other)
        {
            return m_guid.Equals(other.m_guid);
        }

        public override bool Equals(object obj)
        {
            return obj is GKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            return m_guid.GetHashCode();
        }

        public static bool operator ==(GKey left, GKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GKey left, GKey right)
        {
            return !(left == right);
        }
    }
}
