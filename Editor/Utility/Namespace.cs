using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;

namespace JakePerry.Unity
{
    /// <summary>
    /// Representation of a namespace.
    /// </summary>
    public readonly struct Namespace : IComparable<Namespace>, IEquatable<Namespace>
    {
        private static readonly Dictionary<string, string> _baseCache = new();
        private static readonly Dictionary<string, string> _nameCache = new();

        private readonly string m_name;
        private readonly string m_full;

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

        /// <summary>
        /// Indicates whether this namespace is equal to <see cref="None"/>.
        /// </summary>
        public bool IsNone => m_full is null;

        /// <summary>
        /// Indicates whether this namespace is equal to <see cref="Root"/>.
        /// </summary>
        public bool IsRoot => m_full is not null && m_full.Length == 0;

        /// <summary>
        /// Get the base namespace for a nested namespace.
        /// <para/>
        /// For example, "System" is the base namespace for "System.Text".
        /// </summary>
        public Namespace BaseNamespace
        {
            get
            {
                string value = m_full;
                _ = value ?? throw new InvalidOperationException();

                if (value.Length == 0) return None;

                if (!_baseCache.TryGetValue(value, out string @base))
                {
                    int i = value.LastIndexOf('.');

                    if (i < 0) return Root;

                    @base = value.Substring(0, i);
                    _baseCache[value] = @base;
                }

                return GetNamespace(@base);
            }
        }

        /// <summary>
        /// Represents no namespace. This value is effectively <see langword="null"/>.
        /// </summary>
        public static Namespace None => new Namespace(null, null);

        /// <summary>
        /// Represents the root (global) namespace.
        /// </summary>
        public static Namespace Root => new Namespace(string.Empty, string.Empty);

        private Namespace(string name, string full)
        {
            m_name = name;
            m_full = full;
        }

        public int CompareTo(Namespace other)
        {
            return StringComparer.Ordinal.Compare(FullName, other.FullName);
        }

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

        /// <summary>
        /// Indicates whether the current namespace is a child of a base namespace.
        /// <para/>
        /// For example, "System.Text" is a child of "System".
        /// </summary>
        /// <param name="other">The base namespace.</param>
        /// <returns>
        /// <see langword="true"/> if the current namespace is a direct child of
        /// <paramref name="other"/>; Otherwise, <see langword="false"/>/
        /// </returns>
        public bool IsChildOf(Namespace other)
        {
            if (m_full is null || other.m_full is null) return false;
            if (m_full.Length == 0) return false;

            if (other.m_full.Length == 0)
            {
                return m_full.IndexOf('.', StringComparison.Ordinal) < 0;
            }

            return m_full.Length == other.m_full.Length + m_name.Length + 1
                && m_full.StartsWith(other.m_full, StringComparison.Ordinal);
        }

        /// <summary>
        /// Indicates whether the current namespace is a descendant of a base namespace.
        /// <para/>
        /// For example, both "System.Text" &amp; "System.Text.RegularExpressions" are
        /// descendants of "System".
        /// </summary>
        /// <param name="other">The base namespace.</param>
        /// <returns>
        /// <see langword="true"/> if the current namespace is a descendant of
        /// <paramref name="other"/>; Otherwise, <see langword="false"/>/
        /// </returns>
        public bool IsDescendantOf(Namespace other)
        {
            if (m_full is null || other.m_full is null) return false;
            if (m_full.Length == 0) return false;

            if (other.m_full.Length == 0) return true;

            return m_full.Length > other.m_full.Length
                && m_full.StartsWith(other.m_full, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return m_full ?? string.Empty;
        }

        public static Namespace GetNamespace(string value)
        {
            if (value is null) return None;
            if (value.Length == 0) return Root;

            // Caching prevents multiple allocations of the same string split.
            if (!_nameCache.TryGetValue(value, out string name))
            {
                // If no period char is found, this is a base namespace ie. 'System'.
                int i = value.LastIndexOf('.');
                if (i < 0) return new Namespace(value, value);

                name = value.Substring(i + 1);
                _nameCache[value] = name;
            }
            
            return new Namespace(name, value);
        }

        public static Namespace GetNamespace(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            return GetNamespace(type.Namespace);
        }

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void ClearCache()
        {
            _baseCache.Clear();
            _nameCache.Clear();
        }
    }
}
