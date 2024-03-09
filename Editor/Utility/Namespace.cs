using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace JakePerry.Unity
{
    /// <summary>
    /// Representation of a namespace.
    /// </summary>
    public readonly struct Namespace :
        IComparable<Namespace>,
        IEnumerable,
        IEnumerable<Namespace>,
        IEquatable<Namespace>
    {
        public struct NestedNamespaceEnumerator : IEnumerator<Namespace>, IEnumerator
        {
            private readonly Namespace m_namespace;
            private int m_index;

            public Namespace Current => m_namespace.GetNestedNamespace(m_index);

            object IEnumerator.Current => this.Current;

            internal NestedNamespaceEnumerator(Namespace @namespace)
            {
                m_namespace = @namespace;
                m_index = -1;
            }

            public bool MoveNext()
            {
                ++m_index;
                return m_index < m_namespace.NestedCount;
            }

            void IDisposable.Dispose() { }

            void IEnumerator.Reset() { m_index = -1; }
        }

        public struct TypesEnumerator : IEnumerator<Type>, IEnumerator
        {
            private readonly TypeCache.TypeCollection m_types;
            private readonly string m_namespace;

            private int m_index;

            public Type Current => m_types[m_index];

            object IEnumerator.Current => this.Current;

            internal TypesEnumerator(ref TypeCache.TypeCollection types, string @namespace)
            {
                m_types = types;
                m_index = -1;

                // If an empty string is specified, we should use null instead for correct comparison.
                m_namespace = (@namespace?.Length ?? 0) == 0 ? null : @namespace;
            }

            public bool MoveNext()
            {
                int count = m_types.Count;
                while ((++m_index) < count)
                {
                    if (StringComparer.Ordinal.Equals(Current.Namespace, m_namespace))
                    {
                        return true;
                    }
                }

                return false;
            }

            void IDisposable.Dispose() { }

            void IEnumerator.Reset() { m_index = -1; }
        }

        public readonly struct TypesEnumerable : IEnumerable<Type>, IEnumerable
        {
            private readonly string m_namespace;
            private readonly Type m_baseType;

            public TypesEnumerable(string @namespace, Type baseType)
            {
                m_baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
                m_namespace = @namespace;
            }

            public TypesEnumerator GetEnumerator()
            {
                var types = TypeCache.GetTypesDerivedFrom(m_baseType);
                return new TypesEnumerator(ref types, m_namespace);
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
            IEnumerator<Type> IEnumerable<Type>.GetEnumerator() => this.GetEnumerator();
        }

        public readonly struct TypesWithAttributeEnumerable : IEnumerable<Type>, IEnumerable
        {
            private readonly string m_namespace;
            private readonly Type m_attrType;

            public TypesWithAttributeEnumerable(string @namespace, Type attrType)
            {
                m_attrType = attrType ?? throw new ArgumentNullException(nameof(attrType));
                m_namespace = @namespace;
            }

            public TypesEnumerator GetEnumerator()
            {
                var types = TypeCache.GetTypesWithAttribute(m_attrType);
                return new TypesEnumerator(ref types, m_namespace);
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
            IEnumerator<Type> IEnumerable<Type>.GetEnumerator() => this.GetEnumerator();
        }

        private readonly string m_name;
        private readonly string m_full;
        private readonly Namespace[] m_children;

        /// <summary>
        /// Indicates the number of nested namespaces.
        /// </summary>
        public int NestedCount => m_children?.Length ?? 0;

        /// <summary>
        /// The delimited name of this namespace. Example: when <see cref="FullName"/>
        /// is equal to <code>System.Collections.Generic</code>
        /// this property will return <code>Generic</code>
        /// </summary>
        public string Name => m_name;

        /// <summary>
        /// The full name of this namespace. Example:
        /// <code>System.Collections.Generic</code>
        /// </summary>
        public string FullName => m_full;

        internal Namespace(string name, string full, Namespace[] children)
        {
            m_name = name;
            m_full = full;
            m_children = children;
        }

        public int CompareTo(Namespace other)
        {
            return StringComparer.Ordinal.Compare(FullName, other.FullName);
        }

        /// <summary>
        /// Attempt to get a nested namespace with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Delimeted name of the namespace.</param>
        /// <param name="child">Matching namespace, if one is found.</param>
        /// <returns>
        /// <see langword="true"/> if a match is found; Otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetNestedNamespace(ReadOnlySpan<char> name, out Namespace child)
        {
            if (m_children is not null)
                foreach (var c in m_children)
                    if (name.Equals(c.Name, StringComparison.Ordinal))
                    {
                        child = c;
                        return true;
                    }

            child = default;
            return false;
        }

        /// <inheritdoc cref="TryGetNestedNamespace(ReadOnlySpan{char}, out Namespace)"/>
        public bool TryGetNestedNamespace(string name, out Namespace child)
        {
            if (m_children is not null)
                foreach (var c in m_children)
                    if (StringComparer.Ordinal.Equals(c.Name, name))
                    {
                        child = c;
                        return true;
                    }

            child = default;
            return false;
        }

        /// <summary>
        /// Get the nested namespace at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">
        /// Index in range [0..<see cref="NestedCount"/>]
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public Namespace GetNestedNamespace(int index)
        {
            var children = m_children;
            if (children is not null && index > -1 && index < children.Length)
            {
                return children[index];
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public TypesEnumerable EnumerateTypesInNamespace(Type baseType = null)
        {
            return new TypesEnumerable(m_full, baseType ?? typeof(object));
        }

        public TypesWithAttributeEnumerable EnumerateTypesWithAttributeInNamespace(Type attrType)
        {
            return new TypesWithAttributeEnumerable(m_full, attrType);
        }

        public NestedNamespaceEnumerator GetEnumerator()
        {
            return new NestedNamespaceEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        IEnumerator<Namespace> IEnumerable<Namespace>.GetEnumerator() => this.GetEnumerator();

        public bool Equals(Namespace other)
        {
            return StringComparer.Ordinal.Equals(m_full, other.m_full);
        }

        public override bool Equals(object obj)
        {
            return obj is Namespace other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(m_full);
        }
    }
}
