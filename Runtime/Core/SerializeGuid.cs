using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JakePerry.Unity
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct SerializeGuid :
        IComparable,
        IComparable<Guid>,
        IComparable<SerializeGuid>,
        IEquatable<Guid>,
        IEquatable<SerializeGuid>,
        IFormattable
    {
        [FieldOffset(0), NonSerialized] private Guid m_guid;

        [FieldOffset(0), SerializeField] private long _a;
        [FieldOffset(8), SerializeField] private long _b;

        /// <summary>
        /// A string representation of the Guid without parenthesis or hyphens.
        /// This is the format generally used by Unity for asset guids etc.
        /// </summary>
        public string UnityGuidString => m_guid.ToString("N");

        /// <summary>First 8-byte segment.</summary>
        public ulong SegmentA { get { unchecked { return (ulong)_a; } } }

        /// <summary>Second 8-byte segment.</summary>
        public ulong SegmentB { get { unchecked { return (ulong)_b; } } }

        /// <summary>
        /// Indicates whether the current object is equal to the default instance.
        /// </summary>
        public bool IsDefault => _a == 0L && _b == 0L;

        public SerializeGuid(Guid guid)
        {
            _a = _b = 0;
            m_guid = guid;
        }

        public SerializeGuid(string guid) : this(new Guid(guid)) { }

        public SerializeGuid(ulong a, ulong b)
        {
            m_guid = default;
            unchecked { _a = (long)a; }
            unchecked { _b = (long)b; }
        }

        public int CompareTo(Guid other)
        {
            return m_guid.CompareTo(other);
        }

        public int CompareTo(SerializeGuid other)
        {
            return m_guid.CompareTo(other.m_guid);
        }

        public int CompareTo(object obj)
        {
            if (obj is Guid other1)
                return this.CompareTo(other1);

            if (obj is SerializeGuid other2)
                return this.CompareTo(other2);

            return -1;
        }

        public bool Equals(Guid other)
        {
            return m_guid.Equals(other);
        }

        public bool Equals(SerializeGuid other)
        {
            return m_guid.Equals(other.m_guid);
        }

        public override bool Equals(object obj)
        {
            if (obj is Guid other1)
                return this.Equals(other1);

            if (obj is SerializeGuid other2)
                return this.Equals(other2);

            return false;
        }

        public override int GetHashCode()
        {
            return m_guid.GetHashCode();
        }

        public override string ToString()
        {
            return UnityGuidString;
        }

        public string ToString(string format)
        {
            return m_guid.ToString(format);
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return m_guid.ToString(format, provider);
        }

        public static SerializeGuid NewGuid()
        {
            return new SerializeGuid(Guid.NewGuid());
        }

        public static SerializeGuid Deserialize(ulong a, ulong b)
        {
            long la, lb;
            unchecked { la = (long)a; lb = (long)b; }

            return new SerializeGuid() { _a = la, _b = lb };
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only. Attempt to deserialize a <see cref="SerializeGuid"/> represented
        /// by the given <paramref name="property"/>.
        /// </summary>
        public static bool TryDeserializeGuid(UnityEditor.SerializedProperty property, out SerializeGuid guid)
        {
            var a = property.FindPropertyRelative("_a");
            var b = property.FindPropertyRelative("_b");

            if (a == null || b == null)
            {
                guid = default;
                return false;
            }

            unchecked { guid = SerializeGuid.Deserialize((ulong)a.longValue, (ulong)b.longValue); }
            return true;
        }
#endif

        public static implicit operator Guid(SerializeGuid guid)
        {
            return guid.m_guid;
        }

        public static implicit operator SerializeGuid(Guid guid)
        {
            return new SerializeGuid(guid);
        }

        public static implicit operator string(SerializeGuid guid)
        {
            return guid.UnityGuidString;
        }

        public static bool operator ==(SerializeGuid left, SerializeGuid right) => left.Equals(right);
        public static bool operator !=(SerializeGuid left, SerializeGuid right) => !(left == right);

        public static bool operator <(SerializeGuid left, SerializeGuid right) => left.CompareTo(right) < 0;
        public static bool operator <=(SerializeGuid left, SerializeGuid right) => left.CompareTo(right) <= 0;
        public static bool operator >(SerializeGuid left, SerializeGuid right) => left.CompareTo(right) > 0;
        public static bool operator >=(SerializeGuid left, SerializeGuid right) => left.CompareTo(right) >= 0;
    }
}
