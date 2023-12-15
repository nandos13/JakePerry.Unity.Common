using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JakePerry.Unity.FPM
{
    /// <summary>
    /// A base class for a <see cref="ScriptableObject"/> implementation which
    /// stores a collection of named keys paired with unique guids.
    /// </summary>
    public abstract class KeyTableBase : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        private struct KeyDefinition
        {
            public string key;
            public SerializeGuid uid;
        }

        /// <summary>
        /// Comparer implementation for sorting definitions by their key string.
        /// </summary>
        private sealed class DefinitionComparer : IComparer<KeyDefinition>
        {
            public static DefinitionComparer Instance { get; } = new DefinitionComparer();
            public int Compare(KeyDefinition x, KeyDefinition y) => StringComparer.Ordinal.Compare(x.key, y.key);
        }

        private readonly HashSet<string> m_keySet = new HashSet<string>();
        private readonly HashSet<SerializeGuid> m_idSet = new HashSet<SerializeGuid>();

        [SerializeField]
        private List<KeyDefinition> m_data = new List<KeyDefinition>();

        private void Init()
        {
            if (m_data == null)
                m_data = new List<KeyDefinition>();
        }

        private void Sort()
        {
            Init();
            m_data.Sort(DefinitionComparer.Instance);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            /* Do nothing. */
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_keySet.Clear();
            m_idSet.Clear();

            Init();
            Sort();

            int i = -1;
            foreach (var def in m_data)
            {
                int index = ++i;

                var key = SanitizeDisplayName(def.key);
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogError($"Deserialization Error; Found a key definition with a null or empty name string at index {index.ToString()}.", this);
                    continue;
                }

                var uid = def.uid;
                if (uid.Equals((SerializeGuid)default))
                {
                    Debug.LogError($"Deserialization Error; Found a key definition with an invalid identifier at index {index.ToString()}.", this);
                    continue;
                }

                bool didAddKey = m_keySet.Add(key);
                if (!didAddKey)
                {
                    Debug.LogError($"Deserialization Error; Found a duplicated key at index {index.ToString()}; The key will be skipped. Did a bad merge occur?", this);
                }

                if (!m_idSet.Add(uid))
                {
                    Debug.LogError($"Deserialization Error; Found a duplicated identifier at index {index.ToString()}; The key will be skipped. Did a bad merge occur?", this);
                    if (didAddKey)
                    {
                        m_keySet.Remove(key);
                    }
                }
            }
        }

        public bool Contains(SerializeGuid guid)
        {
            return m_idSet.Contains(guid);
        }

        public bool TryFindName(SerializeGuid guid, out string name)
        {
            foreach (var def in m_data)
            {
                if (def.uid.Equals(guid))
                {
                    name = def.key;
                    return true;
                }
            }

            name = string.Empty;
            return false;
        }

        public bool TryFindGuid(string name, out SerializeGuid guid)
        {
            foreach (var def in m_data)
            {
                if (StringComparer.Ordinal.Equals(def.key, name))
                {
                    guid = def.uid;
                    return true;
                }
            }

            guid = default;
            return false;
        }

        public void GetDefinitions(List<(string, SerializeGuid)> list)
        {
            _ = list ?? throw new ArgumentNullException(nameof(list));

            foreach (var s in m_data)
                list.Add((s.key, s.uid));
        }

        /// <summary>
        /// Sanitize a key's display name to remove undesired characters &amp;
        /// replace them with underscores.
        /// </summary>
        public static string SanitizeDisplayName(string s)
        {
            const string pattern = @"[\\~#%&*{}/:<>?|""-]";

            if (s is null) return string.Empty;

            // Remove unwanted characters
            var regex = new Regex(pattern);
            s = regex.Replace(s, "_");

            // Remove whitespace within the name
            s = Regex.Replace(s.Trim(), @"\s+", "_");

            return s.ToLower();
        }
    }
}
